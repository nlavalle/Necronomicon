using System.Diagnostics;
using System.Runtime.CompilerServices;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class InlineArrayTypeDefinition
    : Source2EntityTypeDefinitionBase
{
    private const TypeCode ArrayTypeCode = TypeCode.Object;

    // Construction phase
    private readonly ProtoFlattenedSerializerField_t? _field;
    private readonly string _name;
    private readonly int _innerStart;
    private readonly int _innerLength;
    private readonly int _lengthStart;
    private readonly int _lengthLength;

    // Reservation phase
    private Source2EntityTypeDefinitionBase? _inner = null;
    private int _length = 0;

    // Placement phase

    public override string TypeName => _name;
    public override int AlignOf
    {
        get
        {
            if (IsHeapType)
            {
                return Source2EntityTypeDatabase.AlignOfHeapEntry;
            }

            var inner = _inner;
            Debug.Assert(inner is not null);

            return inner.AlignOf;
        }
    }
    public override bool IsHeapType => _lengthStart < 0;

    public InlineArrayTypeDefinition(ProtoFlattenedSerializerField_t? field, string name, int innerStart, int innerLength, int lengthStart, int lengthLength)
    {
        _field = field;
        _name = name;
        _innerStart = innerStart;
        _innerLength = innerLength;
        _lengthStart = lengthStart;
        _lengthLength = lengthLength;
    }

    protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
    {
        var inner = builder.CreateParsedType(_field, _name, _innerStart, _innerLength);
        int length;
        var lengthSpan = _name.AsSpan(_lengthStart, _lengthLength);
        if (!int.TryParse(lengthSpan, out length))
        {
            length = builder.GetConstValue(lengthSpan);
        }

        inner.Reserve(builder, serDef);

        builder.ReserveType();

        _inner = inner;
        _length = length;
    }

    internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
    {
        int sizeOf = _length;
        var inner = _inner;
        Debug.Assert(inner is not null);

        var sym = builder.GetOrPlaceSymbol(_name);
        var innerPosition = inner.GetOrPlace(builder, Source2EntityInclusionFlags.Include);

        Source2EntityTypeDatabase.HeapOffset[]? offsets = null;
        var innerOffsets = builder.GetHeapOffsetsForPlacedType(innerPosition);
        
        var flags = Source2EntityFlags.Array;
        if (inner.IsHeapType)
        {
            var offsetId = innerPosition;
            if (innerOffsets is not null)
                offsetId = ~offsetId;

            offsets = new Source2EntityTypeDatabase.HeapOffset[sizeOf];
            for (int i = 0; i < sizeOf; i++)
            {
                offsets[i] = new(Unsafe.SizeOf<Source2EntityTypeDatabase.HeapEntry>() * i, offsetId);
            }

            flags |= Source2EntityFlags.Heap;
        }
        else
        {
            if (innerOffsets is not null)
            {
                offsets = new Source2EntityTypeDatabase.HeapOffset[sizeOf * innerOffsets.Length];

                int i = 0;
                while (i < offsets.Length)
                {
                    var addend = inner.SizeOf * i;
                    
                    foreach (var innerOffset in innerOffsets)
                    {
                        offsets[i++] = new(addend + innerOffset.Offset, innerOffset.TypeId);
                    }
                }
            }
        }

        sizeOf *= inner.SizeOf;

        var delly = inner.GetOuterDecoder(Source2EntityFlags.Array);
        type = new(delly, offsets, sym, flags, ArrayTypeCode, sizeOf, innerPosition, _length);

        return sizeOf;
    }
}
