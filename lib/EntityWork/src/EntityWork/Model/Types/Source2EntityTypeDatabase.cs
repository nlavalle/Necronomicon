using System.Diagnostics;
using System.Runtime.CompilerServices;
using EntityWork.Internal.Heap;

namespace EntityWork.Model.Types;

public sealed class Source2EntityTypeDatabase
{
    internal const int AlignOfHeapEntry = sizeof(int);

    internal readonly Type[] _types;
    internal readonly Field[] _fields;
    internal readonly string[] _symbols;
    internal readonly int[] _symbolsLow;
    internal readonly int _classInfoCount;

    internal Source2EntityTypeDatabase(Type[] types, Field[] fields, string[] symbols, int[] symbolsLow, int entityCount)
    {
        _types = types;
        _fields = fields;
        _symbols = symbols;
        _symbolsLow = symbolsLow;
        _classInfoCount = entityCount;
    }

    // public Source2Entity CreateEntityByClassId(int classId)
    // {
    //     ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)classId, (uint)_classInfoCount, nameof(classId));

    //     return new Source2Entity(this, classId + 1);
    // }

    internal ref readonly Type GetTypeById(int typeId)
    {
        Debug.Assert((uint)typeId < (uint)_types.Length);

        return ref _types[typeId];
    }

    internal string GetSymbolById(int symbolId)
    {
        Debug.Assert((uint)symbolId < (uint)_symbols.Length);

        return _symbols[symbolId];
    }

    internal bool TryGetTypeAtOrdinal(int type, int ordinal, out int typeId)
    {
        if ((uint)type >= (uint)_types.Length)
        {
            typeId = default;
            return false;
        }

        ref var typeRef = ref _types[type];

        var count = typeRef.Count;
        if (count >= 0 && (uint)ordinal >= (uint)count)
        {
            typeId = default;
            return false;
        }

        var index = typeRef.Index;
        var flags = typeRef.Flags;

        if ((flags & Source2EntityFlags.Array) == 0)
        {
            ref var fieldRef = ref Unsafe.Add(ref _fields[index], (uint)ordinal);
            index = fieldRef.TypeId;
            flags = fieldRef.Flags;
        }

        if ((flags & Source2EntityFlags.Heap) != 0)
        {
            index = ~index;
        }

        typeId = index;
        return true;
    }

    internal bool TryReshapeTypeAtOrdinal(int type, int ordinal, out TypeReshapeData typeData)
    {
        if ((uint)type >= (uint)_types.Length)
        {
            typeData = default;
            return false;
        }

        ref var typeRef = ref _types[type];

        var count = typeRef.Count;
        if (count >= 0 && (uint)ordinal >= (uint)count)
        {
            typeData = default;
            return false;
        }

        var index = typeRef.Index;
        var flags = typeRef.Flags;
        int sizeOf;
        int offsetOf;

        if ((flags & Source2EntityFlags.Array) != 0)
        {
            typeRef = ref _types[index];

            if ((flags & Source2EntityFlags.Heap) != 0)
            {
                sizeOf = Unsafe.SizeOf<HeapEntry>();
                index = ~index;
            }
            else
            {
                sizeOf = typeRef.SizeOf;
            }

            offsetOf = ordinal * sizeOf;
        }
        else
        {
            ref var fieldRef = ref Unsafe.Add(ref _fields[index], (uint)ordinal);
            index = fieldRef.TypeId;
            flags = fieldRef.Flags;
            typeRef = ref _types[index];

            if ((flags & Source2EntityFlags.Heap) != 0)
            {
                sizeOf = Unsafe.SizeOf<HeapEntry>();
                index = ~index;
            }
            else
            {
                sizeOf = typeRef.SizeOf;
            }

            flags |= typeRef.Flags;

            if ((flags & Source2EntityFlags.Exclude) != 0)
                sizeOf = 0;

            offsetOf = fieldRef.OffsetOf;
        }

        typeData = new TypeReshapeData(offsetOf, sizeOf, index);
        return true;
    }

    public string[] DumpFallbackTypes()
    {
        var dump = new string[_symbolsLow.Length - 1];

        for (int i = 1; i < _symbolsLow.Length; i++)
        {
            var symLow = _symbolsLow[i];
            dump[i - 1] = _symbols[symLow];
        }

        return dump;
    }

    public TypeCode GetTypeCodeForType(int type)
    {
        return _types[type].TypeCode;
    }

    // NULL (0):
    //      Decoder = ThrowDecoder
    //      HeapOffsets = null
    //      Symbol = "NULL"
    //      Flags = 0
    //      TypeCode = None
    //      SizeOf = 0
    //      Index = 0
    //      Count = int.MaxValue
    // Primitive:
    //      Decoder = Decoder
    //      HeapOffsets = null
    //      Symbol = SymbolId
    //      Flags ! (Array | Heap)
    //      TypeCode = TypeCode
    //      SizeOf = SizeOf
    //      Index = 0
    //      Count = 0
    // Inline Object:
    //      Decoder = Decoder?
    //      HeapOffsets = HeapOffsets?
    //      Symbol = SymbolId
    //      Flags ! (Array | Heap)
    //      TypeCode = Object
    //      SizeOf = SUM(Field.SizeOf)
    //      Index = FieldIndex
    //      Count = FieldCount
    // Entity:
    //      Decoder = null
    //      HeapOffsets = HeapOffsets?
    //      Symbol = SymbolId
    //      Flags = Entity ! (Array | Heap)
    //      TypeCode = Object
    //      SizeOf = SUM(Field.SizeOf)
    //      Index = FieldIndex
    //      Count = FieldCount
    // Inline Array:
    //      Decoder = Decoder? (Outer)
    //      HeapOffsets = HeapOffsets?
    //      Symbol = SymbolId
    //      Flags = Array ! Heap
    //      TypeCode = Object || String
    //      SizeOf = ArrayCount * Inner.SizeOf
    //      Index = Inner.TypeId
    //      Count = ArrayCount
    // Dynamic Array:
    //      Decoder = Decoder? (Outer)
    //      HeapOffsets = []? ([] if Inner.HeapOffsets is not null, null otherwise)
    //      Symbol = SymbolId
    //      Flags = Array ! Heap
    //      TypeCode = Object || String
    //      SizeOf = HeapEntry.SizeOf
    //      Index = Inner.TypeId
    //      Count = -1
    // Heaped Object (INVALID/RESERVED):
    //      Decoder = UNKNOWN
    //      HeapOffsets = UNKNOWN
    //      Symbol = SymbolId
    //      Flags = Heap ! Array
    //      TypeCode = UNKNOWN
    //      SizeOf (0) = UNKNOWN
    //      FieldIndex = UNKNOWN
    //      FieldCount = UNKNOWN
    // Inline Array of Heaped Objects:
    //      Decoder = null
    //      HeapOffsets = [0, HeapEntry.SizeOf, ...]
    //      Symbol = SymbolId
    //      Flags = Array | Heap
    //      TypeCode = Object
    //      SizeOf (0) = ArrayCount * HeapEntry.SizeOf
    //      FieldIndex = Inner.TypeId
    //      FieldCount = ArrayCount
    // Dynamic Array of Heaped Objects:
    //      Decoder = null
    //      HeapOffsets = []
    //      Symbol = SymbolId
    //      Flags = Array | Heap
    //      TypeCode = Object
    //      SizeOf (0) = HeapEntry.SizeOf
    //      FieldIndex = Inner.TypeId
    //      FieldCount = -1
    internal readonly record struct Type(EntityFieldDecoder? Decoder, HeapOffset[]? HeapOffsets, int Symbol, Source2EntityFlags Flags, TypeCode TypeCode, int SizeOf, int Index, int Count);

    // NULL (0):
    //      Symbol = "NULL"
    //      Flags = 0
    //      TypeId = 0 (NULL)
    //      OffsetOf = int.MinValue
    // Inline:
    //      Symbol = SymbolId
    //      Flags ! Heap
    //      TypeId = TypeId
    //      OffsetOf = OffsetOf
    // Heap:
    //      Symbol = SymbolId
    //      Flags = Heap
    //      TypeId = HeapedType.TypeId
    //      OffsetOf = HeapEntry.OffsetOf
    internal readonly record struct Field(int Symbol, ushort TypeSymbol, Source2EntityFlags Flags, int TypeId, int OffsetOf);

    // Inline Field containing Heaped Type => Field(Heap) -> Type
    // Inline Field containing Heaped Dynamic Array => Field(Heap) -> Type(Array!Heap) -> Type
    // Inline Array of Heaped Types => Field(!Heap) -> Type(Array|Heap) -> Type
    // Excluded Types and Fields reshape as: SizeOf = 0, even if type's size is not 0
    // Excluded Fields do not take up inline space, and will not cause heap allocations when requested
    // Excluded Inline Types do not take up inline space, and will not cause heap allocations when requested
    // Excluded Entities will not allocate or create a heap, but are present in the type database
    // Builtin Primitives and Types cannot be Type-Excluded and must be Field-Excluded
    
    internal readonly record struct TypeReshapeData(int OffsetOf, int SizeOf, int Type);

    internal struct HeapEntry
    {
        internal AllocPlateEntry Alloc;
    }

    internal readonly record struct HeapOffset(int Offset, int TypeId);
}
