using System;
using System.IO;

namespace FileParser
{
    public static class Utils
    {
        /// <summary>
        /// Search for a string in a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="stringToSearch">The string as an array of bytes.</param>
        /// <returns>The offset of the string in the buffer, or -1 if not found.</returns>
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

        public static string GetAbsoluteFolderPath(string basePath, string? folder = null)
        {
            var fullyQualifiedPath = Path.IsPathFullyQualified(basePath) ?
                 basePath
                : Path.Combine(Environment.CurrentDirectory, basePath);

            if (!string.IsNullOrWhiteSpace(folder))
            {
                fullyQualifiedPath = Path.Combine(fullyQualifiedPath, folder);
            }

            return fullyQualifiedPath;
        }
    }
}
