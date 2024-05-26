using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wordle_server
{
    public static class Logic
    {
        public static string[] GetFeedBack(string guess, string answer)
        {
            if (guess == answer)
                return  new string[5] { "green", "green", "green", "green", "green" };

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
    }
}
