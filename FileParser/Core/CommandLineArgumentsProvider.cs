using System;

namespace FileParser.Core
{
    public sealed class CommandLineArgumentsProvider : ICommandLineArgumentsProvider
    {
        string[] ICommandLineArgumentsProvider.Arguments => Environment.GetCommandLineArgs();
    }
}
