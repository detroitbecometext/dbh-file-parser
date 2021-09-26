namespace FileParser.Tests.Helpers
{
    public class MockTranslationValue
    {
        public MockTranslationValueType Type { get; init; }
        public string Key { get; init; }
        public string Value { get; init; }

        public string FormattedValue
        {
            get
            {
                if(Type == MockTranslationValueType.Choice)
                {
                    return $"μ{(char)0}{Value}{(char)2}{(char)0}{(char)0}";
                }
                else
                {
                    return $"μ{(char)0}{{S}}{Value}{(char)1}{(char)0}{(char)0}";
                }
            }
        }
    }
}
