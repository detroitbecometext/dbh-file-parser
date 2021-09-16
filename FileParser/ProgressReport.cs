using System;
using FileParser.Config;

namespace FileParser
{
    public class ProgressReport
    {
        public string FileName { get; init; } = string.Empty;
        public int SectionIndex { get; init; }
        public int SectionCount { get; init; }
        public int LanguageIndex { get; init; }

        private double Percentage { get => Math.Round((float)(LanguageIndex + 1) / Configuration.Languages.Length * 100); }

        public override string ToString()
        {
            if(SectionIndex == SectionCount - 1 && LanguageIndex == Configuration.Languages.Length - 1)
            {
                return $"\"{FileName}\": Done !";
            }
            else
            {
                return $"\"{FileName}\": Section {SectionIndex + 1} of {SectionCount} -- {Percentage}% of section processed...";
            }
        }
    }
}
