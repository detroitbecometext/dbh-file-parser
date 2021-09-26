namespace FileParser.Core
{
    public interface IConsoleWriter
    {
        public void WriteLine(string text);
        public void WriteErrorLine(string text);
        public void ClearLine(int line);
    }
}
