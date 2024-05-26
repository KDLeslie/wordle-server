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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email; 
            if (data.Email == null)
                userId = req.Cookies["userId"];
            if (userId == null)
                return new BadRequestObjectResult("Null user ID.");

            string guess = new string(data.Guess);
            string answer = await StorageHandler.GetAnswer(userId, data.SessionToken);

            #if DEBUG
            log.LogInformation(answer);
            #endif
            string[] colours = Logic.GetFeedBack(guess, answer);
            return new OkObjectResult(new { colours });
        }

        [FunctionName("GetAnswer")]
        public static async Task<IActionResult> RunGetAnswer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email;
            if (data.Email == null)
                userId = req.Cookies["userId"];
            if (userId == null)
                return new BadRequestObjectResult("Null user ID.");

            string word = await StorageHandler.GetAnswer(userId, data.SessionToken);
            return new OkObjectResult(new { word });
        }

        [FunctionName("ValidateGuess")]
        public static async Task<IActionResult> RunValidateGuess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string guess = new string(data.Guess);

            HashSet<string> hashSet = await StorageHandler.GetValidWords();
            bool valid = hashSet.Contains(guess);
            return new OkObjectResult(new { valid });
        }

        [FunctionName("SetSession")]
        public static async Task<IActionResult> RunSetSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email; 
            if (data.Email == null)
                userId = req.Cookies["userId"];
            if (userId == null)
                return new BadRequestObjectResult("Null user ID.");

            await StorageHandler.StoreSession(userId, data.SessionToken);

            return new OkObjectResult(new {success = true});
        }

        [FunctionName("GetGUID")]
        public static async Task<IActionResult> RunGetGUID(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
        {
            string guid = Guid.NewGuid().ToString();
            return new OkObjectResult(new { guid });
        }

        [FunctionName("IncrementNumerator")]
        public static async Task<IActionResult> RunIncrementNumerator(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email;
            if (data.Email == null)
                userId = req.Cookies["userId"];
            if (userId == null)
                return new BadRequestObjectResult("Null user ID.");

            string score = await StorageHandler.IncrementNumerator(userId);
            return new OkObjectResult(new { score });
        }

        [FunctionName("IncrementDenominator")]
        public static async Task<IActionResult> RunIncrementDenominator(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email;
            if (data.Email == null)
                userId = req.Cookies["userId"];
            if (userId == null)
                return new BadRequestObjectResult("Null user ID.");

            string score = await StorageHandler.IncrementDenominator(userId);
            return new OkObjectResult(new { score });
        }

        [FunctionName("GetRatio")]
        public static async Task<IActionResult> RunGetRatio(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email;
            if (data.Email == null)
                userId = req.Cookies["userId"];
            if (userId == null)
            {
                string score = "0/0";
                return new OkObjectResult(new { score });
            }
            else
            {
                string score = await StorageHandler.GetRatio(userId) ?? "0/0";
                return new OkObjectResult(new { score });
            }
        }
        [FunctionName("GetGoogleClientID")]
        public static async Task<IActionResult> RunGetGoogleClientID(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
        {
            string clientID = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            return new OkObjectResult(new { clientID });
        }
    }

    public class Request
    {
        public char[] Guess { get; set; }
        public string SessionToken { get; set; }
        public string Email { get; set; }
    }
}
