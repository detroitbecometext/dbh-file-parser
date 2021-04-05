using System;
using System.Collections.Generic;
using System.Text;

namespace TextExtractor.Config
{
    public class SectionConfig
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public List<string> Keys { get; set; }

        public int Length => EndOffset - StartOffset;
    }
}
