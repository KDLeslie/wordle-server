using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace wordle_server
{
    public class APIs
    {
        private readonly IGameLogicService _gameLogicService;
        private readonly IStorageService _storageService;
        private readonly IIdentifierService _identifierService;

        public APIs(IGameLogicService gameLogicService, IStorageService storageService, IIdentifierService identifierService)
        {
            _gameLogicService = gameLogicService;
            _storageService = storageService;
            _identifierService = identifierService;
        }

        [FunctionName("CheckGuess")]
        public async Task<IActionResult> RunCheckGuess(
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
            #if DEBUG
            log.LogInformation(await _storageService.GetAnswer(userId, data.SessionToken));
            #endif
            string[] colours = await _gameLogicService.CheckGuess(userId, data.SessionToken, guess);

            return new OkObjectResult(new { colours });
        }

        [FunctionName("GetAnswer")]
        public async Task<IActionResult> RunGetAnswer(
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

            string word = await _storageService.GetAnswer(userId, data.SessionToken);

            return new OkObjectResult(new { word });
        }

        [FunctionName("ValidateGuess")]
        public async Task<IActionResult> RunValidateGuess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string guess = new string(data.Guess);

            bool valid = await _gameLogicService.ValidateGuess(guess);

            return new OkObjectResult(new { valid });
        }

        [FunctionName("SetSession")]
        public async Task<IActionResult> RunSetSession(
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

            await _storageService.SetSession(userId, data.SessionToken);

            return new OkObjectResult(new {success = true});
        }

        [FunctionName("GetGUID")]
        public async Task<IActionResult> RunGetGUID(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
        {

            string guid = _identifierService.GetGUID();
            return new OkObjectResult(new { guid });
        }

        [FunctionName("IncrementNumerator")]
        public async Task<IActionResult> RunIncrementNumerator(
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

            string score = await _storageService.IncrementNumerator(userId);

            return new OkObjectResult(new { score });
        }

        [FunctionName("IncrementDenominator")]
        public async Task<IActionResult> RunIncrementDenominator(
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

            string score = await _storageService.IncrementDenominator(userId);

            return new OkObjectResult(new { score });
        }

        [FunctionName("GetRatio")]
        public async Task<IActionResult> RunGetRatio(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Request data = JsonConvert.DeserializeObject<Request>(requestBody);
            string userId = data.Email;
            if (data.Email == null)
                userId = req.Cookies["userId"];
            
            string score = await _storageService.GetRatio(userId);
            
            return new OkObjectResult(new { score });
            
        }

        [FunctionName("GetGoogleClientID")]
        public async Task<IActionResult> RunGetGoogleClientID(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
        {
            string clientID = _identifierService.GetGoogleClientId();

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
