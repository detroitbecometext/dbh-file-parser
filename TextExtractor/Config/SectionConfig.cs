using System.Collections.Generic;

namespace TextExtractor.Config
{
    public class SectionConfig
    {
        /// <summary>
        /// The offset of the start of this section.
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// The offset of the end of this section.
        /// </summary>
        public int EndOffset { get; set; }

        /// <summary>
        /// True if every language's list of keys ends with a listing of said keys.
        /// </summary>
        public bool HasKeyListing { get; set; }

        /// <summary>
        /// The list of translation keys for this section.
        /// </summary>
        public List<string> Keys { get; set; }

        /// <summary>
        /// The length of this section.
        /// </summary>
        public int Length => EndOffset - StartOffset;
    }
}
