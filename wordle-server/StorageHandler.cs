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

        public static CloudTable getTable(string tableName)
        {
            // Create or retrieve the CloudTable instance
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            return tableClient.GetTableReference(tableName);
        }
        public static async Task<CloudTable> getOrCreateTable(string tableName)
        {
            CloudTable table = getTable(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }
        public static CloudBlockBlob getBlockBlob(string containerName, string blobName) 
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            return blob;
        }

        public static async Task<HashSet<string>> getValidWords()
        {
            CloudBlockBlob blob = getBlockBlob(containerName, validWordsStorageName);
            string blobText = await blob.DownloadTextAsync();
            string[] words = blobText.Split(new string[] { "\n" }, StringSplitOptions.None);
            HashSet<string> hashSet = new HashSet<string>(words);
            return hashSet;

        }
        public static async Task<string> getAnswer(string id)
        {
            CloudTable table = getTable(tableName);
            await table.CreateIfNotExistsAsync();

            TableOperation retrieveOperation = TableOperation.Retrieve<MyEntity>(id, id);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            MyEntity entity = (MyEntity)result.Result;
            return entity.Word;
        }
        public static async Task storeSession(string id)
        {

            CloudBlockBlob blob = getBlockBlob(containerName, possibleAnswersStorageName);
            CloudTable table = getTable(tableName);
            await table.CreateIfNotExistsAsync();

            string blobText = await blob.DownloadTextAsync();

            string[] words = blobText.Split(new string[] { "\n" }, StringSplitOptions.None);

            Random rnd = new Random();
            int i = rnd.Next(0, words.Length);
            string word = words[i];


            // Create a new entity to be inserted into the table
            MyEntity entity = new MyEntity
            {
                PartitionKey = id, // Your partition key
                RowKey = id, // Unique row key
                Word = word, // Replace with your property names
            };

            // Create the TableOperation that inserts the entity
            TableOperation insertOperation = TableOperation.Insert(entity);

            // Execute the insert operation
            await table.ExecuteAsync(insertOperation);
        }
        public class MyEntity : TableEntity
        {
            public string Word { get; set; }
        }

    }
}
