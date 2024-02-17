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
            string[] Colours = new string[5];
            if (guess == answer)
            {
                return  new string[5] { "green", "green", "green", "green", "green" };
            }

            // Keep track of letters in the answer that have and have not been guessed correctly
            Dictionary<char, int> lettersLeft = new Dictionary<char, int>();
            foreach (char letter in answer)
            {
                if (!lettersLeft.ContainsKey(letter))
                    lettersLeft[letter] = 1;
                else
                    lettersLeft[letter]++;
            }

            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == answer[i])
                {
                    Colours[i] = "green";
                    lettersLeft[guess[i]]--;
                }
                else if (lettersLeft.ContainsKey(guess[i]) && lettersLeft[guess[i]] != 0)
                {
                    Colours[i] = "yellow";
                    lettersLeft[guess[i]]--;
                }
                else
                {
                    Colours[i] = "grey";
                }
            }

            // Fixing bug where letter count is negative
            for (int i = 0; i < 5; i++)
            {
                if (lettersLeft.ContainsKey(guess[i]) && lettersLeft[guess[i]] < 0)
                {
                    Colours[i] = "grey";
                    lettersLeft[guess[i]]++;
                }
            }
            return Colours;
        }
    }
}
