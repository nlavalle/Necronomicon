using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EntityWork.Model.Types;

internal sealed class Source2FieldDefinition
{
    // Construction phase
    private readonly int _fieldIndex;

    // Reservation phase
    private Source2EntityTypeDefinitionBase? _type;

    // Placement phase
    public Source2EntityInclusionFlags Flags { get; set; }
    internal bool IsHeapOffset { get; private set; }
    internal Source2EntityTypeDatabase.HeapOffset[]? InnerHeapOffsets { get; private set; }

    public Source2FieldDefinition(int index)
    {
        _fieldIndex = index;
    }

    internal (int offset, int alignOf) Place(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, int offset, ref Source2EntityTypeDatabase.Field field)
    {
        var alignOf = 1;
        var postOffset = offset;
        var type = _type;
        Debug.Assert(type is not null);

        var typeId = type.GetOrPlace(builder, Source2EntityInclusionFlags.Include);
        InnerHeapOffsets = builder.GetHeapOffsetsForPlacedType(typeId);

        var serializer = builder.Serializer;
        var fie = serializer.Fields[_fieldIndex];

        var sym = builder.GetOrPlaceSymbol(serializer.Symbols[fie.VarNameSym]);

        ushort symLow = 0;
        if (type.IsFallbackType)
        {
            symLow = builder.GetOrPlaceSymbolLow(serializer.Symbols[fie.VarTypeSym]);
        }

        int sizeOf = type.SizeOf;
        var flags = Source2EntityDatabaseBuilder.ConvertFlagsForField(parentFlags);
        if (sizeOf != 0 && (flags & Source2EntityFlags.Exclude) == 0)
        {
            if (type.IsHeapType)
            {
                sizeOf = Unsafe.SizeOf<Source2EntityTypeDatabase.HeapEntry>();
                alignOf = Source2EntityTypeDatabase.AlignOfHeapEntry;
                flags |= Source2EntityFlags.Heap;
                IsHeapOffset = true;
            }
            else
            {
                alignOf = type.AlignOf;
            }

            Debug.Assert(alignOf > 0);
            Debug.Assert(int.IsPow2(alignOf));

            var mask = alignOf - 1;
            offset = (offset + mask) & ~mask;
            postOffset = offset + sizeOf;
        }
        else
        {
            if (type.IsHeapType)
                flags |= Source2EntityFlags.Heap;
        }

        field = new(sym, symLow, flags, typeId, offset);

        Debug.Assert(alignOf > 0);

        return (postOffset, alignOf);
    }

    internal void Reserve(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
    {
        var serializer = builder.Serializer;
        var fie = serializer.Fields[_fieldIndex];
        var typeSym = serializer.Symbols[fie.VarTypeSym];

        var type = builder.CreateParsedType(fie, typeSym);
        type.Reserve(builder, serDef);
        _type = type;
    }
}
