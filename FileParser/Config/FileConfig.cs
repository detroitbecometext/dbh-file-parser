using System.Collections.Generic;

namespace FileParser.Config
{
    public class FileConfig
    {
        public string Name { get; set; } = string.Empty;
        public List<SectionConfig> Sections { get; set; } = new();
    }
}
