namespace FileParser.Core.Tests.Helpers;

internal class MockTranslationValue
{
    public required MockTranslationValueType Type { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }

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
