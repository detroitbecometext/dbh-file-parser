using System;
using System.Collections.Generic;
using System.Text;

namespace TextExtractor.Config
{
    public class Configuration
    {
        public static readonly IEnumerable<string> Languages = new List<string>() {
                "FRE",
                "ENG",
                "GER",
                "ITA",
                "SPA",
                "DUT",
                "POR",
                "SWE",
                "DAN",
                "NOR",
                "FIN",
                "RUS",
                "POL",
                "JPN",
                "KOR",
                "CHI",
                "GRE",
                "CZE",
                "HUN",
                "CRO",
                "MEX",
                "BRA",
                "TUR",
                "ARA",
        };

        public List<FileConfig> Files { get; set; }
    }
}
