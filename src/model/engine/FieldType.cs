using System.Text.RegularExpressions;

namespace necronomicon.model.engine;

public class FieldType
{
    public string BaseType { get; set; } = string.Empty;
    public FieldType? GenericType { get; set; }
    public bool Pointer { get; set; }
    public int Count { get; set; }

    private static readonly Regex FieldTypeRegex = new Regex(
        @"([^\<\[\*]+)(\<\s*(.*?)\s*\>)?(\*)?(\[(.*?)\])?",
        RegexOptions.Compiled
    );

    private static readonly Dictionary<string, int> ItemCounts = new Dictionary<string, int>
    {
        {"MAX_ITEM_STOCKS", 8 },
        {"MAX_ABILITY_DRAFT_ABILITIES", 48}
    };

    public static FieldType Parse(string name)
    {
        var match = FieldTypeRegex.Match(name);
        if (!match.Success || match.Groups.Count != 7)
        {
            throw new ArgumentException($"bad regexp: {name} -> {match.Groups.Count} groups");
        }

        var fieldType = new FieldType
        {
            BaseType = match.Groups[1].Value,
            Pointer = match.Groups[4].Value == "*"
        };

        if (!string.IsNullOrEmpty(match.Groups[3].Value))
        {
            fieldType.GenericType = Parse(match.Groups[3].Value);
        }

        var countStr = match.Groups[6].Value;
        if (!string.IsNullOrEmpty(countStr))
        {
            if (ItemCounts.TryGetValue(countStr, out int predefinedCount))
            {
                fieldType.Count = predefinedCount;
            }
            else if (int.TryParse(countStr, out int countVal) && countVal > 0)
            {
                fieldType.Count = countVal;
            }
            else
            {
                fieldType.Count = 1024;
            }
        }

        return fieldType;
    }
}