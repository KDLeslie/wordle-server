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
        static readonly string tableName = "sessions";
        static readonly string containerName = "words";
        static readonly string validWordsStorageName = "valid-wordle-words.txt";
        static readonly string possibleAnswersStorageName = "answerlist.txt";

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
        public static async Task<string> GetAnswer(string id)
        {
            CloudTable table = GetTable(tableName);

            TableOperation retrieveOperation = TableOperation.Retrieve<SessionEntity>(id, id);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            SessionEntity entity = (SessionEntity)result.Result;
            return entity.Word;
        }
        public static async Task StoreSession(string id)
        {
            CloudBlockBlob blob = GetBlockBlob(containerName, possibleAnswersStorageName);
            CloudTable table = await GetOrCreateTable(tableName);

            string blobText = await blob.DownloadTextAsync();
            string[] words = blobText.Split(new string[] { "\n" }, StringSplitOptions.None);

            Random rnd = new Random();
            int i = rnd.Next(0, words.Length);
            string word = words[i];

            SessionEntity entity = new SessionEntity
            {
                PartitionKey = id,
                RowKey = id,
                Word = word,
            };

            TableOperation insertOperation = TableOperation.Insert(entity);
            await table.ExecuteAsync(insertOperation);
        }
        public class SessionEntity : TableEntity
        {
            public string Word { get; set; }
        }
    }
}
