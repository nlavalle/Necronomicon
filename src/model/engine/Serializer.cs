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

    public FieldDecoder GetDecoderForFieldPath(FieldPath fieldPath, int position)
    {
        var index = fieldPath.Path[position];
        if (index >= Fields.Count)
        {
            throw new NecronomiconException($"Serializer: {Name} Field Path {fieldPath.Path} has no field {index}");
        }

        return Fields[index].GetDecoderForFieldPath(fieldPath, position + 1);
    }
}