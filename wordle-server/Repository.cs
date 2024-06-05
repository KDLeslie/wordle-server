using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;

namespace wordle_server
{
    public static class StorageHandler
    {
        static readonly string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        static readonly string tableName = Environment.GetEnvironmentVariable("TABLE_NAME");
        static readonly string containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");
        static readonly string validWordsStorageName = Environment.GetEnvironmentVariable("VALID_WORDS_BLOB");
        static readonly string possibleAnswersStorageName = Environment.GetEnvironmentVariable("ANSWERS_BLOB");

        public static CloudTable GetTable(string tableName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            return tableClient.GetTableReference(tableName);
        }

        public static async Task<CloudTable> GetOrCreateTable(string tableName)
        {
            CloudTable table = GetTable(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public static CloudBlockBlob GetBlockBlob(string containerName, string blobName) 
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            return blob;
        }

        public static async Task<HashSet<string>> GetValidWords()
        {
            CloudBlockBlob blob = GetBlockBlob(containerName, validWordsStorageName);
            string blobText = await blob.DownloadTextAsync();
            string[] words = blobText.Split(new string[] { "\n" }, StringSplitOptions.None);
            HashSet<string> hashSet = new HashSet<string>(words);
            return hashSet;
        }

        public static async Task<string> GetAnswer(string userId, string sessionId)
        {
            CloudTable table = GetTable(tableName);

            TableOperation retrieveOperation = TableOperation.Retrieve<SessionEntity>(userId, sessionId);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            SessionEntity entity = (SessionEntity)result.Result;
            return entity.Answer;
        }

        public static async Task<string> GetRatio(string userId)
        {
            CloudTable table = GetTable(tableName);

            TableOperation retrieveOperation = TableOperation.Retrieve<ScoreEntity>(userId, userId);
            TableResult result = await table.ExecuteAsync(retrieveOperation);

            if (result.Result != null)
            {
                ScoreEntity entity = (ScoreEntity)result.Result;
                return entity.Score;
            }
            else
            {
                return null;
            }
        }

        public static async Task<string> GetRandomWord()
        {
            CloudBlockBlob blob = GetBlockBlob(containerName, possibleAnswersStorageName);

            string blobText = await blob.DownloadTextAsync();
            string[] words = blobText.Split(new string[] { "\n" }, StringSplitOptions.None);

            Random rnd = new Random();
            int i = rnd.Next(0, words.Length);
            return words[i];
        }

        public static async Task StoreSession(string userId, string sessionId)
        {
            CloudTable table = await GetOrCreateTable(tableName);

            string answer = await GetRandomWord();

            SessionEntity entity = new SessionEntity
            {
                PartitionKey = userId,
                RowKey = sessionId,
                Answer = answer
            };

            TableOperation insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation);
        }

        public static async Task SetScore(string userId, string score)
        {

            CloudTable table = await GetOrCreateTable(tableName);

            ScoreEntity entity = new ScoreEntity
            {
                PartitionKey = userId,
                RowKey = userId,
                Score = score
            };

            TableOperation retrieveOperation = TableOperation.Retrieve<ScoreEntity>(userId, userId);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            ScoreEntity existingEntity = (ScoreEntity)result.Result;

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

        public static async Task<string> IncrementNumerator(string userId)
        {
            string ratio = await GetRatio(userId);
            if (await GetRatio(userId) == null)
            {
                await SetScore(userId, "1/0");
                return "1/0";
            }
                
            int num = Int32.Parse(ratio.Split('/')[0]);
            int denum = Int32.Parse(ratio.Split('/')[1]);
            num++;
            string score = $"{num}/{denum}";
            await SetScore(userId, score);
            return score.ToString();
        }

        public static async Task<string> IncrementDenominator(string userId)
        {
            string ratio = await GetRatio(userId);
            if (await GetRatio(userId) == null)
            {
                await SetScore(userId, "0/1");
                return "0/1";
            }
            int num = Int32.Parse(ratio.Split('/')[0]);
            int denum = Int32.Parse(ratio.Split('/')[1]);
            denum++;
            string score = $"{num}/{denum}";
            await SetScore(userId, score);
            return score.ToString();
        }

        public class SessionEntity : TableEntity
        {
            public string Answer { get; set; }
        }

        public class ScoreEntity : TableEntity
        {
            public string Score { get; set; }
        }
    }
}
