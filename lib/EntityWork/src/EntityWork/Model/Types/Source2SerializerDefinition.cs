namespace EntityWork.Model.Types;

internal sealed class Source2SerializerDefinition
    : Source2EntityTypeDefinitionBase
{
    // Construction phase
    private readonly int _serializerIndex;
    private readonly Source2EntityInclusionFlags[] _fieldFlags;
    private readonly string _name;

    // Reservation phase
    public int HeapCount { get; private set; } = 0;

    // Placement phase
    public Source2EntityInclusionFlags Flags { get; set; }
    private int _alignOf = -1;

    public override string TypeName => _name;
    public override int AlignOf => _alignOf;

    internal Source2SerializerDefinition(Source2EntityDatabaseBuilder builder, int serializerIndex)
    {
        var ser = builder.Serializer.Serializers[serializerIndex];
        _serializerIndex = serializerIndex;

        _name = builder.Serializer.Symbols[ser.SerializerNameSym];

        var count = ser.FieldsIndex.Count;

        _fieldFlags = new Source2EntityInclusionFlags[count];
    }

    protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
    {
        var fieldCount = _fieldFlags.Length;

        builder.ReserveTypeAndFieldCount(fieldCount);
        var ser = builder.Serializer.Serializers[_serializerIndex];
        var fields = ser.FieldsIndex;

        for (int i = 0; i < fields.Count; i++)
        {
            var field = builder.GetFieldDefinition(fields[i]);
            field.Reserve(builder, this);
        }
    }

    internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
    {
        List<Source2EntityTypeDatabase.HeapOffset> heapOffsets = new();
        int sizeOf = 0;
        int alignOfOuter = 1;

        var thisFlags = Source2EntityDatabaseBuilder.CalculateFlagsForThis(parentFlags, Flags);
        var inheritFlags = Source2EntityDatabaseBuilder.CalculateFlagsForChild(thisFlags, Flags);
        var fromFields = Source2EntityInclusionFlags.None;

        var fieldCount = _fieldFlags.Length;
        var ser = builder.Serializer.Serializers[_serializerIndex];

        var fields = builder.AllocateFields(fieldCount, out var fieldIndex);
        var fies = ser.FieldsIndex;
        for (int i = 0; i < fies.Count; i++)
        {
            var fie = builder.GetFieldDefinition(fies[i]);

            // Calculate flags: Type[parent] -> Field (global) -> Field (type)
            var fieFlags = fie.Flags;
            fromFields |= Source2EntityDatabaseBuilder.CalculateFlagsForChild(fieFlags, fieFlags);

            var fieldFlags = Source2EntityDatabaseBuilder.CalculateFlagsForThis(inheritFlags, fieFlags);
            fieldFlags = Source2EntityDatabaseBuilder.CalculateFlagsForChild(fieldFlags, fieFlags);
            fieldFlags = Source2EntityDatabaseBuilder.CalculateFlagsForThis(fieldFlags, _fieldFlags[i]);

            ref var fieldRef = ref fields[i];

            (sizeOf, var alignOfInner) = fie.Place(builder, fieldFlags, sizeOf, ref fieldRef);

            var checkFlags = fieldRef.Flags;
            if ((checkFlags & Source2EntityFlags.Exclude) == 0)
            {
                var innerOffsets = builder.GetHeapOffsetsForPlacedType(fieldRef.TypeId);

                if ((checkFlags & Source2EntityFlags.Heap) != 0)
                {
                    var innerTypeId = fieldRef.TypeId;
                    if (innerOffsets is not null)
                        innerTypeId = ~innerTypeId;

                    heapOffsets.Add(new(fieldRef.OffsetOf, innerTypeId));
                }
                else
                {
                    if (innerOffsets is not null)
                    {
                        var addend = fieldRef.OffsetOf;
                        foreach (var innerOffset in innerOffsets)
                        {
                            heapOffsets.Add(new(addend + innerOffset.Offset, innerOffset.TypeId));
                        }
                    }
                }
            }

            alignOfOuter = Math.Max(alignOfOuter, alignOfInner);
        }

        if ((Flags & Source2EntityInclusionFlags.NoInheritSet) == 0)
            thisFlags |= fromFields & Source2EntityInclusionFlags.IncludeSet;

        var sym = builder.GetOrPlaceSymbol(builder.Serializer.Symbols[ser.SerializerNameSym]);
        Source2EntityTypeDatabase.HeapOffset[]? heaps = null;
        if (heapOffsets.Count != 0)
            heaps = heapOffsets.ToArray();

        var mask = alignOfOuter - 1;
        sizeOf = (sizeOf + mask) & ~mask;

        type = new(null, heaps, sym, Source2EntityDatabaseBuilder.ConvertFlagsForType(thisFlags, Source2EntityFlags.None), TypeCode.Object, sizeOf, fieldIndex, fieldCount);

        _alignOf = alignOfOuter;
        
        return sizeOf;
    }

    public int ReserveHeap()
    {
        return ++HeapCount;
    }
}
