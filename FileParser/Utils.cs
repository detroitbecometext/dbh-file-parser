using System.Collections.Generic;

namespace FileParser
{
    public static class Utils
    {
        /// <summary>
        /// Search for a byte pattern in a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="pattern">The bytes to look for.</param>
        /// <returns>The offsets of the pattern in the buffer.</returns>
        public static IEnumerable<int> FindOffsets(byte[] buffer, byte[] pattern)
        {
            for (var i = 0; i <= (buffer.Length - pattern.Length); i++)
            {
                if (buffer[i] == pattern[0])
                {
                    for (int j = 1; j < pattern.Length && buffer[i + j] == pattern[j]; j++)
                    {
                        if (j == pattern.Length - 1)
                        {
                            yield return i;
                        }
                    }
                }
            }
        }
    }
}
