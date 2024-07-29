namespace FileParser.Core.Utils;

/// <summary>
/// Contains utility methods.
/// </summary>
internal static class BinaryUtils
{
    /// <summary>
    /// Search for instances of a byte pattern in a buffer.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="pattern">The bytes to look for.</param>
    /// <returns>The offsets of the patterns in the buffer.</returns>
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

    /// <summary>
    /// Reads a 32-bit integer from a binary reader in big-endian format.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <returns>The parsed int.</returns>
    public static int ReadInt32BigEndian(BinaryReader reader)
    {
        return ReadInt32BigEndian(reader.ReadBytes(4));
    }

    /// <summary>
    /// Reads a 32-bit integer from a byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">The byte array.</param>
    /// <returns>The parsed int.</returns>
    /// <exception cref="ArgumentException">If the provided array is not 4 bytes long.</exception>
    public static int ReadInt32BigEndian(byte[] bytes)
    {
        if (bytes.Length != 4)
        {
            throw new ArgumentException("Provided array must be 4 bytes long");
        }

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToInt32(bytes, 0);
    }
}
