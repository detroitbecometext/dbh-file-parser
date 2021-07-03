using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextExtractor
{
    public static class Utils
    {
        /// <summary>
        /// Returns the offset of the string in the buffer, or -1 if not found.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="stringToSearch"></param>
        /// <returns></returns>
        public static int SearchStringInBuffer(byte[] buffer, byte[] stringToSearch)
        {
            for (var i = 0; i <= (buffer.Length - stringToSearch.Length); i++)
            {
                if (buffer[i] == stringToSearch[0])
                {
                    for (int j = 1; j < stringToSearch.Length && buffer[i + j] == stringToSearch[j]; j++)
                    {
                        if (j == stringToSearch.Length - 1)
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        public static void ClearLine(int line)
        {
            Console.SetCursorPosition(0, line);
            Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));
            Console.SetCursorPosition(0, line);
        }
    }
}
