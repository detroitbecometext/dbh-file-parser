using System;
using System.Reflection;

namespace FileParser.Core
{
    public sealed class CommandLineArgumentsProvider : ICommandLineArgumentsProvider
    {
        public string[] Arguments => Environment.GetCommandLineArgs();
        public string ExecutablePath => Assembly.GetExecutingAssembly().Location ?? string.Empty;
    }
}
