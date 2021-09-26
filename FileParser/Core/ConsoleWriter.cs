using System;

namespace FileParser.Core
{
    public class ConsoleWriter : IConsoleWriter
    {
        void IConsoleWriter.ClearLine(int line)
        {
            Console.SetCursorPosition(0, line);
            Console.Out.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));
            Console.SetCursorPosition(0, line);
        }

        void IConsoleWriter.WriteErrorLine(string text)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(text);
            Console.ForegroundColor = defaultColor;
        }

        void IConsoleWriter.WriteLine(string text)
        {
            Console.Out.WriteLine(text);
        }
    }
}
