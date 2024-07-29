using System.IO.Abstractions;

namespace FileParser.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IFileSystem"/>.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Given a base path and a filename, returns the absolute path to the file.
    /// </summary>
    /// <param name="basePath">The base path. Can be absolute or relative.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The absolute path to the file.</returns>
    public static string GetAbsolutePath(this IFileSystem fileSystem, string basePath, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("The base path cannot be empty.");
        }

        var fullyQualifiedPath = fileSystem.Path.GetFullPath(fileSystem.Path.IsPathFullyQualified(basePath) ?
             basePath
            : fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), basePath));

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            fullyQualifiedPath = fileSystem.Path.Combine(fullyQualifiedPath, fileName);
        }

        return fullyQualifiedPath;
    }
}
