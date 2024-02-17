using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System.Xml;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace wordle_server
{
    public static class APIs
    {
        [FunctionName("CheckGuess")]
        public static async Task<IActionResult> RunCheckGuess(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Guess data = JsonConvert.DeserializeObject<Guess>(requestBody);
            string guess = new string(data.Chars);
            string answer = await StorageHandler.getAnswer(data.Token);


            FeedBack feedBack = new FeedBack();
            feedBack.Colours = Logic.GetFeedBack(guess, answer);

            return new OkObjectResult(feedBack);
        }

        [FunctionName("GetAnswer")]
        public static async Task<IActionResult> RunGetAnswer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Guess data = JsonConvert.DeserializeObject<Guess>(requestBody);
            Answer answer = new Answer();
            answer.Word =  await StorageHandler.getAnswer(data.Token);

            return new OkObjectResult(answer);
        }

        [FunctionName("ValidateGuess")]
        public static async Task<IActionResult> RunValidateGuess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Word data = JsonConvert.DeserializeObject<Word>(requestBody);

            HashSet<string> hashSet = await StorageHandler.getValidWords();
            ValidWord validWord = new ValidWord();
            validWord.Valid = hashSet.Contains(new string(data.Chars));

            return new OkObjectResult(validWord);
        }


        [FunctionName("SetSession")]
        public static async Task<IActionResult> RunSetSession(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            SessionToken data = JsonConvert.DeserializeObject<SessionToken>(requestBody);
            await StorageHandler.storeSession(data.Token);

            return new OkObjectResult($"Gucci.");
        }
    }
    public class Answer
    {
        public string Word { get; set; }
    }

    public class ValidWord
    {
        public bool Valid { get; set; }
    }

    public class Word
    {
        public char[] Chars {  get; set; }
    }

    public class FeedBack
    {
        public string[] Colours { get; set; }
    }

    public class WordEntity : TableEntity
    {
        public string Word { get; set; }
    }
    public class SessionToken
    {
        public string Token { get; set;}
    }
    public class Guess
    {
        public char[] Chars { get; set; }
        public string Token { get; set; }

    }
}
