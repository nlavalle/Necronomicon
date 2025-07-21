using Steam.Protos.Dota2;

namespace necronomicon.model.engine;

public enum FieldModel
{
    Simple = 0,
    FixedArray = 1,
    FixedTable = 2,
    VariableArray = 3,
    VariableTable = 4
}

public class Field
{
    public string ParentName { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public string VarType { get; set; } = string.Empty;
    public string SendNode { get; set; } = string.Empty;
    public string SerializerName { get; set; } = string.Empty;
    public int SerializerVersion { get; set; }
    public string Encoder { get; set; } = string.Empty;
    public int? EncodeFlags { get; set; }
    public int? BitCount { get; set; }
    public float? LowValue { get; set; }
    public float? HighValue { get; set; }
    public FieldType? FieldType { get; set; }
    public Serializer? Serializer { get; set; }
    public object? Value { get; set; }
    public FieldModel Model { get; set; }

    public FieldDecoder? Decoder { get; set; }
    public FieldDecoder? BaseDecoder { get; set; }
    public FieldDecoder? ChildDecoder { get; set; }

    public Field(CSVCMsg_FlattenedSerializer serializer, ProtoFlattenedSerializerField_t field)
    {
        VarName = resolve(serializer, field.VarNameSym);
        VarType = resolve(serializer, field.VarTypeSym);
        SendNode = resolve(serializer, field.SendNodeSym);
        SerializerName = resolve(serializer, field.FieldSerializerNameSym);
        SerializerVersion = field.FieldSerializerVersion;
        Encoder = resolve(serializer, field.VarEncoderSym);
        EncodeFlags = field.EncodeFlags;
        BitCount = field.BitCount;
        LowValue = field.LowValue;
        HighValue = field.HighValue;
        Model = FieldModel.Simple;

        if (SendNode == "(root)")
        {
            SendNode = string.Empty;
        }
    }

    private string resolve(CSVCMsg_FlattenedSerializer serializer, int? index)
    {
        if (index == null) return string.Empty;
        return serializer.Symbols[(int)index];
    }
}