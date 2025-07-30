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
        VarName = field.HasVarNameSym ? resolve(serializer, field.VarNameSym) : string.Empty;
        VarType = field.HasVarTypeSym ? resolve(serializer, field.VarTypeSym) : string.Empty;
        SendNode = field.HasSendNodeSym ? resolve(serializer, field.SendNodeSym) : string.Empty;
        SerializerName = field.HasFieldSerializerNameSym ? resolve(serializer, field.FieldSerializerNameSym) : string.Empty;
        SerializerVersion = field.FieldSerializerVersion;
        Encoder = field.HasVarEncoderSym ? resolve(serializer, field.VarEncoderSym) : string.Empty;
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

    public void SetModel(FieldModel model)
    {
        Model = model;
        switch (model)
        {
            case FieldModel.FixedArray:
                Decoder = FieldDecoders.FindDecoder(this);
                break;
            case FieldModel.FixedTable:
                BaseDecoder = FieldDecoders.BooleanDecoder;
                break;
            case FieldModel.VariableArray:
                if (FieldType?.GenericType == null)
                {
                    throw new NecronomiconException($"No generic type for Variable Array Field {FieldType}");
                }
                BaseDecoder = FieldDecoders.UnsignedDecoder;
                ChildDecoder = FieldDecoders.FindDecoderByBaseType(FieldType.GenericType.BaseType);
                break;
            case FieldModel.VariableTable:
                BaseDecoder = FieldDecoders.UnsignedDecoder;
                break;
            case FieldModel.Simple:
                Decoder = FieldDecoders.FindDecoder(this);
                break;
        }
    }

    public FieldDecoder GetDecoderForFieldPath(FieldPath fieldPath, int position)
    {
        switch (Model)
        {
            case FieldModel.FixedArray:
                if (Decoder == null) throw new NecronomiconException("FixedArray field expected Decoder to not be null");
                return Decoder;
            case FieldModel.FixedTable:
                if (fieldPath.Last == position - 1)
                {
                    if (BaseDecoder == null) throw new NecronomiconException("FixedTable field expected BaseDecoder to not be null");
                    return BaseDecoder;
                }
                if (Serializer == null) throw new NecronomiconException("FixedTable field expected Serializer to not be null");
                return Serializer.GetDecoderForFieldPath(fieldPath, position);
            case FieldModel.VariableArray:
                if (fieldPath.Last == position)
                {
                    if (ChildDecoder == null) throw new NecronomiconException("VariableArray field expected ChildDecoder to not be null");
                    return ChildDecoder;
                }
                if (BaseDecoder == null) throw new NecronomiconException("VariableArray field expected BaseDecoder to not be null");
                return BaseDecoder;
            case FieldModel.VariableTable:
                if (fieldPath.Last >= position + 1)
                {
                    if (Serializer == null) throw new NecronomiconException("VariableTable field expected Serializer to not be null");
                    return Serializer.GetDecoderForFieldPath(fieldPath, position + 1);
                }
                if (BaseDecoder == null) throw new NecronomiconException("VariableTable field expected BaseDecoder to not be null");
                return BaseDecoder;
        }

        if (Decoder == null) throw new NecronomiconException("Default Field field expected Decoder to not be null");
        return Decoder;
    }
}