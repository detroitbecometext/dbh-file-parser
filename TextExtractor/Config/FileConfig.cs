using System;
using System.Collections.Generic;
using System.Text;

namespace TextExtractor.Config
{
    public class FileConfig
    {
        public string Name { get; set; }
        public List<SectionConfig> Sections { get; set; }
    }
}
