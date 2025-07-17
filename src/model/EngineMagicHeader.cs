namespace necronomicon.model;

public enum EngineMagicHeader
{
    SOURCE_2,
    DOTA_SOURCE_1,
    CSGO_SOURCE_1
}

public static class EngineMagicHeaderExtensions
{
    private static readonly Dictionary<string, EngineMagicHeader> _stringToEnum = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PBDEMS2\0"] = EngineMagicHeader.SOURCE_2,
        ["PBUFDEM\0"] = EngineMagicHeader.DOTA_SOURCE_1,
        ["HL2DEMO\0"] = EngineMagicHeader.CSGO_SOURCE_1
    };

    public static bool TryParseStringValue(string str, out EngineMagicHeader engineMagicHeader) =>
        _stringToEnum.TryGetValue(str, out engineMagicHeader);    
}