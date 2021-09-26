using System.Collections.Generic;

namespace FileParser.Config
{
    public class FileConfig
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The sections to extract.
        /// </summary>
        public List<SectionConfig> Sections { get; set; } = new();
    }
}
