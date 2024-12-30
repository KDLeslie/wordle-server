using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace wordle_server
{
    public interface IGameLogicService
    {
        Task<bool> ValidateGuess(string guess);

        string[] GetFeedBack(string guess, string answer);

        Task<string[]> CheckGuess(string userId, string sessionId, string guess);
    }

    public class GameLogicService : IGameLogicService
    {
        readonly IStorageService _storageService;
        readonly IBlobDAO _blobDAO;

        public GameLogicService(IStorageService storageService, IBlobDAO blobDAO)
        {
            _storageService = storageService;
            _blobDAO = blobDAO;
        }

        public async Task<bool> ValidateGuess(string guess)
        {
            HashSet<string> hashSet = await _blobDAO.GetValidWordsAsync();
            return hashSet.Contains(guess);
        }

        public string[] GetFeedBack(string guess, string answer)
        {
            if (guess == answer)
                return new string[5] { "green", "green", "green", "green", "green" };

            // Keep track of letters in the answer that have and have not been guessed correctly
            Dictionary<char, int> lettersLeft = new Dictionary<char, int>();
            foreach (char letter in answer)
            {
                if (!lettersLeft.ContainsKey(letter))
                    lettersLeft[letter] = 1;
                else
                    lettersLeft[letter]++;
            }

            string[] colours = new string[5];
            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == answer[i])
                {
                    colours[i] = "green";
                    lettersLeft[guess[i]]--;
                }
                else if (lettersLeft.ContainsKey(guess[i]) && lettersLeft[guess[i]] != 0)
                {
                    colours[i] = "yellow";
                    lettersLeft[guess[i]]--;
                }
                else
                {
                    colours[i] = "grey";
                }
            }

            // Hack to fix bug where yellows should be grey if a duplicate letter is alredy in corrrect spot
            // (ex: guessing harsh when answer is flush had the first letter be yellow and not grey)
            for (int i = 0; i < 5; i++)
            {
                if (lettersLeft.ContainsKey(guess[i]) && lettersLeft[guess[i]] < 0)
                {
                    colours[i] = "grey";
                    lettersLeft[guess[i]]++;
                }
            }
            return colours;
        }

        public async Task<string[]> CheckGuess(string userId, string sessionId, string guess)
        {
            string answer = await _storageService.GetAnswer(userId, sessionId);
            return GetFeedBack(guess, answer);
        }
    }

    public interface IStorageService
    {
        Task<string> GetAnswer(string userId, string sessionId);

        Task<string> GetRatio(string userId);

        Task SetSession(string userId, string sessionId);

        Task RemoveAllSessions(string userId);

        Task RemoveSession(string userId, string sessionId);

        Task<string> IncrementNumerator(string userId);

        Task<string> IncrementDenominator(string userId);
    }

    public class StorageService : IStorageService
    {
        readonly IBlobDAO _blobDAO;
        readonly ITableDAO _tableDAO;

        public StorageService(IBlobDAO blobDAO, ITableDAO tableDAO, ILogger logger)
        {
            _blobDAO = blobDAO;
            _tableDAO = tableDAO;
        }

        private async Task<string> GetRandomWord()
        {
            string[] words = await _blobDAO.GetPossibleAnswersAsync();

            Random rnd = new Random();
            int i = rnd.Next(0, words.Length);
            return words[i];
        }

        public async Task<string> GetRatio(string userId)
        {
            // NOTE: might be unneccesary
            if (userId == null)
            {
                return "0/0";
            }
            else
            {
                (_, string score) = await _tableDAO.Get(userId, userId);
                return score ?? "0/0";
            }
        }

        public async Task<string> GetAnswer(string userId, string sessionId)
        {
            (string answer, _) = await _tableDAO.Get(userId, sessionId);
            return answer;
        }

        public async Task SetSession(string userId, string sessionId)
        {
            string answer = await GetRandomWord();
            await _tableDAO.Insert(userId, sessionId, answer);
        }

        public async Task RemoveAllSessions(string userId)
        {
            await _tableDAO.DeleteAll(userId);
        }

        public async Task RemoveSession(string userId, string sessionId)
        {
            await _tableDAO.Delete(userId, sessionId);
        }

        public async Task<string> IncrementNumerator(string userId)
        {
            string ratio = await GetRatio(userId);
            if (await GetRatio(userId) == null)
            {
                await _tableDAO.Insert(userId, userId, score : "1/0");
                return "1/0";
            }

            int num = Int32.Parse(ratio.Split('/')[0]);
            int denum = Int32.Parse(ratio.Split('/')[1]);
            num++;
            string score = $"{num}/{denum}";
            await _tableDAO.Update(userId, userId, score : score);
            return score;
        }

        public async Task<string> IncrementDenominator(string userId)
        {
            string ratio = await GetRatio(userId);
            if (await GetRatio(userId) == null)
            {
                await _tableDAO.Insert(userId, userId, score : "0/1");
                return "0/1";
            }

            int num = Int32.Parse(ratio.Split('/')[0]);
            int denum = Int32.Parse(ratio.Split('/')[1]);
            denum++;
            string score = $"{num}/{denum}";
            await _tableDAO.Update(userId, userId, score : score);
            return score;
        }
    }

    public interface IIdentifierService
    {
        string GetGUID();

        string GetGoogleClientId();
    }

    public class IdentifierService : IIdentifierService
    {
        public string GetGUID()
        {
            return Guid.NewGuid().ToString();
        }

        public string GetGoogleClientId()
        {
            return Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        }
    }
}
