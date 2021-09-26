using System.Collections.Generic;

namespace FileParser.Config
{
    public class Configuration
    {
        public static readonly string[] Languages = new string[]
        {
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

        public List<FileConfig> Files { get; set; } = new();
    }
}
