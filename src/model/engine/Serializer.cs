using Google.Protobuf.WellKnownTypes;

namespace necronomicon.model.engine;

public class Serializer
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<Field> Fields { get; set; } = new List<Field>();

    public Serializer(string name, int version)
    {
        Name = name;
        Version = version;
        Fields = new List<Field>();
    }
}