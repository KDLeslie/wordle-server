using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wordle_server
{
    public interface IBlobDAO
    {
        Task<HashSet<string>> GetValidWordsAsync();
        Task<string[]> GetPossibleAnswersAsync();
    }

    public class BlobDAO : IBlobDAO
    {
        private static readonly string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        private static readonly string containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");
        private static readonly string validWordsStorageName = Environment.GetEnvironmentVariable("VALID_WORDS_BLOB");
        private static readonly string possibleAnswersStorageName = Environment.GetEnvironmentVariable("ANSWERS_BLOB");

        private readonly Lazy<Task<HashSet<string>>> _lazyValidWords;
        private readonly Lazy<Task<string[]>> _lazyPossibleAnswers;

        public BlobDAO()
        {
            _lazyValidWords = new Lazy<Task<HashSet<string>>>(() => LoadValidWordsAsync(validWordsStorageName));
            _lazyPossibleAnswers = new Lazy<Task<string[]>>(() => LoadBlobAsync(possibleAnswersStorageName));
        }

        public Task<HashSet<string>> GetValidWordsAsync()
        {
            return _lazyValidWords.Value;
        }

        public Task<string[]> GetPossibleAnswersAsync()
        {
            return _lazyPossibleAnswers.Value;
        }

        private static CloudBlockBlob GetBlockBlob(string containerName, string blobName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            return blob;
        }

        private static async Task<HashSet<string>> LoadValidWordsAsync(string blobName)
        {
            CloudBlockBlob blob = GetBlockBlob(containerName, blobName);
            string blobText = await blob.DownloadTextAsync();
            var words = new HashSet<string>(blobText.Split(new[] { "\n" }, StringSplitOptions.None));
            return words;
        }

        private static async Task<string[]> LoadBlobAsync(string blobName)
        {
            CloudBlockBlob blob = GetBlockBlob(containerName, blobName);
            string blobText = await blob.DownloadTextAsync();
            return blobText.Split(new[] { "\n" }, StringSplitOptions.None);
        }
    }

    public interface ITableDAO
    {
        Task<(string, string)> Get(string partitionKey, string rowKey);
        Task Insert(string partitionKey, string rowKey, string answer = null, string score = null);
        Task Update(string partitionKey, string rowKey, string answer = null, string score = null);
        Task Delete(string partitionKey, string rowKey);
        Task DeleteAll(string partitionKey, bool keepScore = true);
    }
    
    public class TableDAO : ITableDAO
    {
        private static readonly string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        private static readonly string tableName = Environment.GetEnvironmentVariable("TABLE_NAME");

        private readonly Lazy<Task<CloudTable>> _lazyTable;

        public TableDAO()
        {
            _lazyTable = new Lazy<Task<CloudTable>>(() => GetOrCreateTableAsync(tableName));
        }

        public Task<CloudTable> GetTableAsync()
        {
            return _lazyTable.Value;
        }

        private async Task<CloudTable> GetOrCreateTableAsync(string tableName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<(string, string)> Get(string partitionKey, string rowKey)
        {
            CloudTable table = await GetTableAsync();

            TableOperation retrieveOperation = TableOperation.Retrieve<Entity>(partitionKey, rowKey);
            TableResult result = await table.ExecuteAsync(retrieveOperation);

            if (result.Result != null)
            {
                Entity entity = (Entity)result.Result;
                return (entity.Answer, entity.Score);
            }
            else
            {
                return (null, null);
            }
        }

        public async Task Insert(string partitionKey, string rowKey, string answer = null, string score = null)
        {
            CloudTable table = await GetTableAsync();

            Entity entity = new()
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Answer = answer,
                Score = score
            };
            TableOperation insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation);
        }

        public async Task Update(string partitionKey, string rowKey, string answer = null, string score = null)
        {
            CloudTable table = await GetTableAsync();

            Entity entity = new()
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Answer = answer,
                Score = score
            };

            TableOperation retrieveOperation = TableOperation.Retrieve<Entity>(partitionKey, rowKey);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            Entity existingEntity = (Entity)result.Result;

            if (existingEntity != null)
            {
                entity.ETag = existingEntity.ETag;
                TableOperation replaceOperation = TableOperation.Replace(entity);
                await table.ExecuteAsync(replaceOperation);
            }
            else
            {
                TableOperation insertOperation = TableOperation.Insert(entity);
                await table.ExecuteAsync(insertOperation);
            }
        }

        public async Task Delete(string partitionKey, string rowKey)
        {
            CloudTable table = await GetTableAsync();

            TableOperation retrieveOperation = TableOperation.Retrieve<Entity>(partitionKey, rowKey);
            TableResult result = await table.ExecuteAsync(retrieveOperation);

            Entity entity = (Entity)result.Result;
            retrieveOperation = TableOperation.Delete(entity);
            await table.ExecuteAsync(retrieveOperation);
        }

        public async Task DeleteAll(string partitionKey, bool keepScore = true)
        {
            CloudTable table = await GetTableAsync();

            TableQuery<Entity> query = new TableQuery<Entity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<Entity> resultSegment = await table.ExecuteQuerySegmentedAsync(query, token);
                token = resultSegment.ContinuationToken;

                foreach (Entity entity in resultSegment.Results)
                {
                    if (entity.RowKey != partitionKey || !keepScore)
                    {
                        TableOperation deleteOperation = TableOperation.Delete(entity);
                        await table.ExecuteAsync(deleteOperation);
                    }
                }
            } while (token != null);
        }

        public class Entity : TableEntity
        {
            public string Answer { get; set; }

            public string Score { get; set; }
        }
    }
}
