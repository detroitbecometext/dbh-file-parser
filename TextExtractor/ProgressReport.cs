using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TextExtractor.Config;

namespace TextExtractor
{
    public class ProgressReport
    {
        public string FileName { get; init; }
        public int SectionIndex { get; init; }
        public int SectionCount { get; init; }
        public int LanguageIndex { get; init; }

        public override string ToString()
        {
            if(SectionIndex == SectionCount - 1 && LanguageIndex == Configuration.Languages.Count() - 1)
            {
                return $"\"{FileName}\": Done !";
            }
            else
            {
                return $"\"{FileName}\": Section {SectionIndex + 1} of {SectionCount} -- {((float)(LanguageIndex + 1) / Configuration.Languages.Count()) * 100}% of section processed...";
            }
        }
    }
}
