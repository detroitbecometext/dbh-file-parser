namespace FileParser.Core.Extraction;

/// <summary>
/// Callbacks called during the extraction process.
/// </summary>
public class ExtractionCallbacks
{
    /// <summary>
    /// Called when all buffer are extracted from the files. The argument is the number of buffers extracted.
    /// </summary>
    public required Action<int> OnBuffersDone { get; init; }

    /// <summary>
    /// Called when a buffer is processed.
    /// </summary>
    public required Action OnBufferProcessed { get; init; }

    /// <summary>
    /// Called when output files are being written. The argument is the number of files to write.
    /// </summary>
    public required Action<int> OnFileWriteStarted { get; init; }

    /// <summary>
    /// Called when an output file is written.
    /// </summary>
    public required Action OnFileWritten { get; init; }

    /// <summary>
    /// Called when an error occurs.
    /// </summary>
    public required Action<string, Exception> OnError { get; init; }

    /// <summary>
    /// Called when a warning occurs.
    /// </summary>
    public required Action<string> OnWarning { get; init; }

    public static ExtractionCallbacks Empty => new()
    {
        OnBuffersDone = _ => { },
        OnBufferProcessed = () => { },
        OnFileWriteStarted = _ => { },
        OnFileWritten = () => { },
        OnError = (_, _) => { },
        OnWarning = _ => { }
    };
}
