using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EntityWork.Model.Types;

internal sealed class DynamicArrayTypeDefinition
    : Source2EntityTypeDefinitionBase
{
    private readonly Source2EntityTypeDefinitionBase _inner;
    private readonly string _name;

    public override string TypeName => _name;
    public override int AlignOf => Source2EntityTypeDatabase.AlignOfHeapEntry;
    public override bool IsHeapType => true;

    public DynamicArrayTypeDefinition(Source2EntityTypeDefinitionBase inner, string name)
    {
        _inner = inner;
        _name = name;
    }

    protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
    {
        builder.ReserveType();
        _inner.Reserve(builder, serDef);
    }

    internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
    {
        var sizeOf = Unsafe.SizeOf<Source2EntityTypeDatabase.HeapEntry>();
        var inner = _inner;
        Debug.Assert(inner is not null);

        var sym = builder.GetOrPlaceSymbol(_name);
        var innerPosition = inner.GetOrPlace(builder, Source2EntityInclusionFlags.Include);

        Source2EntityTypeDatabase.HeapOffset[]? offsets = null;

        var flags = Source2EntityFlags.Array;
        if (inner.IsHeapType)
        {
            offsets = [];
            flags |= Source2EntityFlags.Heap;
        }
        else
        {
            var innerOffsets = builder.GetHeapOffsetsForPlacedType(innerPosition);
            if (innerOffsets is not null)
            {
                offsets = [];
            }
        }

        var delly = inner.GetOuterDecoder(Source2EntityFlags.Array | Source2EntityFlags.Heap);
        type = new(delly, offsets, sym, flags, TypeCode.Object, sizeOf, innerPosition, -1);

        return sizeOf;
    }
}
