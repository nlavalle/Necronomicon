using System.Diagnostics;
using System.Reflection;
using EntityWork.Model.Types;

namespace EntityWork.Diagnostics;

public sealed class Source2EntityTypeDatabaseDump
    : IDisposable
{
    private readonly Source2EntityTypeDatabase _db;
    private readonly int[] _refs;
    private readonly List<int> _heaps;
    private readonly Dictionary<EntityFieldDecoder, List<int>> _decoders;
    private readonly TextWriter _writer;

    public Source2EntityTypeDatabaseDump(Source2EntityTypeDatabase db, string path)
    {
        _db = db;
        var types = db._types;

        _heaps = new();
        _refs = new int[types.Length];
        _decoders = new();

        for (int i = 0; i < types.Length; i++)
        {
            ref var typeRef = ref types[i];

            var flags = typeRef.Flags;
            if ((flags & Source2EntityFlags.Array) != 0)
            {
                _refs[typeRef.Index]++;
            }

            if (typeRef.HeapOffsets is not null)
            {
                _heaps.Add(i);
            }

            var delly = typeRef.Decoder;
            if (delly is not null)
            {
                if (!_decoders.TryGetValue(delly, out var list))
                {
                    list = new();
                    _decoders.Add(delly, list);
                }

                list.Add(i);
            }
        }

        var fields = db._fields;
        for (int i = 0; i < fields.Length; i++)
        {
            ref var fieldRef = ref fields[i];

            _refs[fieldRef.TypeId]++;
        }

        _writer = new StreamWriter(path);
    }

    public void DumpAll()
    {
        DumpEntities();
        _writer.WriteLine();
        DumpHeaps();
        _writer.WriteLine();
        DumpDelegates();
    }

    public void DumpEntities()
    {
        _writer.WriteLine("== Entities ({0}) ==", _db._classInfoCount);

        for (int i = 1; i <= _db._classInfoCount; i++)
            DumpEntity(i);
    }

    public void DumpHeaps()
    {
        _writer.WriteLine("== Heaps ({0}) ==", _heaps.Count);

        for (int i = 0; i < _heaps.Count; i++)
            DumpHeap(_heaps[i]);
    }

    public void DumpDelegates()
    {
        _writer.WriteLine("== Delegates ({0}) ==", _decoders.Count);

        foreach (var kvp in _decoders)
        {
            DumpDelegate(kvp);
        }
    }

    public void DumpEntity(int typeId)
    {
        scoped Span<char> buffer = stackalloc char[512];
        ref var typeRef = ref _db._types[typeId];
        var entityId = typeId - 1;
        var sym = typeRef.Symbol;
        var symbol = _db._symbols[sym];

        _writer.WriteLine("{0} ({1}) SizeOf: {2}, Heap: {3}", symbol, entityId, typeRef.SizeOf, typeRef.HeapOffsets is not null ? typeRef.HeapOffsets.Length : 0);

        DumpTypeFields(buffer, typeId, 4, 0);
    }

    private void DumpTypeFields(scoped Span<char> buffer, int typeId, int indent = 0, int offset = 0)
    {
        buffer.Slice(0, indent).Fill(' ');
        scoped var buf = buffer.Slice(indent);

        ref var typeRef = ref _db._types[typeId];
        var flags = typeRef.Flags;
        if ((flags & Source2EntityFlags.Array) != 0)
        {

        }
        else
        {
            var fields = _db._fields;
            var index = typeRef.Index;

            for (int i = 0; i < typeRef.Count; i++)
            {
                var fieldId = index + i;
                var len = DumpTypeField(buf, fieldId, out var recurse, offset);
                _writer.WriteLine(buffer.Slice(0, 512 - len));
                if (recurse >= 0)
                {
                    DumpTypeFields(buffer, recurse, indent + 4, offset + _db._fields[fieldId].OffsetOf);
                }
            }
        }
    }

    private int DumpTypeField(scoped Span<char> span, int fieldId, out int recurse, int offset = 0)
    {
        ref var fieldRef = ref _db._fields[fieldId];
        ref var typeRef = ref _db._types[fieldRef.TypeId];

        span = span.Slice(WriteFields(span, fieldRef.Flags | (typeRef.Flags & Source2EntityFlags.Array)));

        span[0] = '+';
        span = span.Slice(1);

        if (!(fieldRef.OffsetOf + offset).TryFormat(span, out var written, "D5"))
            throw new Exception();

        span[written] = ' ';
        span = span.Slice(written + 1);

        var sym = fieldRef.Symbol;
        var symbol = _db._symbols[sym];

        symbol.CopyTo(span);
        span = span.Slice(symbol.Length);

        span[0] = ' ';
        span[1] = ':';
        span[2] = ' ';

        span = span.Slice(3);

        sym = fieldRef.TypeSymbol;
        if (sym != 0)
        {
            sym = _db._symbolsLow[sym];
            symbol = _db._symbols[sym];

            symbol.CopyTo(span);
            span = span.Slice(symbol.Length);

            span[0] = ' ';
            span[1] = '[';
            span = span.Slice(2);

            sym = typeRef.Symbol;
            symbol = _db._symbols[sym];

            symbol.CopyTo(span);
            span = span.Slice(symbol.Length);

            span[0] = ']';
            span = span.Slice(1);
        }
        else
        {
            sym = typeRef.Symbol;
            symbol = _db._symbols[sym];

            symbol.CopyTo(span);
            span = span.Slice(symbol.Length);
        }

        span[0] = ' ';
        span[1] = '(';
        span = span.Slice(2);

        if (!fieldRef.TypeId.TryFormat(span, out written))
            throw new Exception();

        span[written] = ')';
        span = span.Slice(written + 1);

        span[0] = ' ';
        span = span.Slice(1);

        if (!typeRef.SizeOf.TryFormat(span, out written))
            throw new Exception();

        span = span.Slice(written);
        // FLAGS +Offset FieldName (FieldId) : TypeName [HiddenTypeName] (TypeId) SIZEOF

        if ((typeRef.Flags & Source2EntityFlags.Array) == 0)
        {
            recurse = fieldRef.TypeId;
        }
        else
        {
            recurse = -1;
        }

        return span.Length;
    }

    private static int WriteFields(scoped Span<char> span, Source2EntityFlags flags)
    {
        Debug.Assert(span.Length >= 4);

        if ((flags & Source2EntityFlags.Exclude) == 0)
        {
            span[0] = 'I';
        }
        else
        {
            span[0] = 'E';
        }

        if ((flags & Source2EntityFlags.Array) == 0)
        {
            span[1] = '-';
        }
        else
        {
            span[1] = 'A';
        }

        if ((flags & Source2EntityFlags.Heap) == 0)
        {
            span[2] = '-';
        }
        else
        {
            span[2] = 'H';
        }

        span[3] = ' ';

        return 4;
    }

    public void DumpHeap(int typeId)
    {
        scoped Span<char> buffer = stackalloc char[512];
        ref readonly var typeRef = ref _db.GetTypeById(typeId);
        var sym = typeRef.Symbol;
        var symbol = _db._symbols[sym];

        _writer.WriteLine("{0} ({1})", symbol, typeId);
        DumpHeapOffsets(buffer, typeId, 4);
    }

    private void DumpHeapOffsets(scoped Span<char> buffer, int typeId, int indent = 0)
    {
        ref var typeRef = ref _db._types[typeId];

        var offsets = typeRef.HeapOffsets;
        if (offsets is not null)
        {
            if (offsets.Length == 0)
            {
                var len = DumpDynamicArrayOffset(buffer, typeRef.Index, out var recurse, indent);
                _writer.WriteLine(buffer.Slice(0, 512 - len));
                if (recurse >= 0)
                {
                    DumpHeapOffsets(buffer, recurse, indent + 4);
                }
            }
            else
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    var len = DumpHeapOffset(buffer, offsets[i], out var recurse, indent);
                    _writer.WriteLine(buffer.Slice(0, 512 - len));
                    if (recurse >= 0)
                    {
                        DumpHeapOffsets(buffer, recurse, indent + 4);
                    }
                }
            }
        }
    }

    private int DumpDynamicArrayOffset(scoped Span<char> buffer, int typeId, out int recurse, int indent = 0)
    {
        const string iterate = "ITERATE (";

        buffer.Slice(0, indent).Fill(' ');
        scoped var span = buffer.Slice(indent);

        iterate.CopyTo(span);
        span = span.Slice(iterate.Length);

        if (!typeId.TryFormat(span, out var written))
            throw new Exception();

        span = span.Slice(written);

        span[0] = ')';
        span[1] = ' ';
        span = span.Slice(2);

        ref var typeRef = ref _db._types[typeId];
        var sym = typeRef.Symbol;
        var symbol = _db._symbols[sym];

        symbol.CopyTo(span);
        span = span.Slice(symbol.Length);

        if (typeRef.HeapOffsets is not null)
        {
            recurse = typeId;
        }
        else
        {
            recurse = -1;
        }

        return span.Length;
    }

    private int DumpHeapOffset(scoped Span<char> buffer, Source2EntityTypeDatabase.HeapOffset offset, out int recurse, int indent = 0)
    {
        buffer.Slice(0, indent).Fill(' ');
        scoped var span = buffer.Slice(indent);

        span[0] = '+';
        span = span.Slice(1);

        if (!offset.Offset.TryFormat(span, out var written, "D5"))
            throw new Exception();

        span = span.Slice(written);

        span[0] = ' ';
        span[1] = '(';
        span = span.Slice(2);

        var innerId = offset.TypeId;
        if (innerId < 0)
        {
            innerId = ~innerId;
            recurse = innerId;
        }
        else
        {
            recurse = -1;
        }

        if (!innerId.TryFormat(span, out written))
            throw new Exception();

        span = span.Slice(written);

        span[0] = ')';
        span[1] = ' ';
        span = span.Slice(2);

        ref var typeRef = ref _db._types[innerId];
        var sym = typeRef.Symbol;
        var symbol = _db._symbols[sym];

        symbol.CopyTo(span);
        span = span.Slice(symbol.Length);

        return span.Length;
    }

    private void DumpDelegate(KeyValuePair<EntityFieldDecoder, List<int>> kvp)
    {
        scoped Span<char> buffer = stackalloc char[512];

        var method = kvp.Key.Method;
        var target = kvp.Key.Target;
        string name = method.Name;

        if (target is not null)
        {
            var type = target.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);

            name += "(";

            foreach (var field in fields)
            {
                name += field.Name;
                name += ":";
                name += field.GetValue(target);
                name += ", ";
            }

            name += ")";
        }

        var declType = method.DeclaringType;
        if (declType is not null)
        {
            name = declType.Name + "." + name;
        }

        _writer.WriteLine(name);
        // FromTypeName (TypeId) Refs: COUNT
        foreach (var typeId in kvp.Value)
        {
            DumpDelegateRef(buffer, typeId, 4);
        }
    }

    public void DumpDelegateRef(scoped Span<char> buffer, int typeId, int indent = 0)
    {
        const string refs = " Refs: ";

        buffer.Slice(0, indent).Fill(' ');
        scoped var buf = buffer.Slice(indent);

        ref var typeRef = ref _db._types[typeId];
        var sym = typeRef.Symbol;
        var symbol = _db._symbols[sym];

        symbol.CopyTo(buf);
        buf = buf.Slice(symbol.Length);

        buf[0] = ' ';
        buf[1] = '(';
        buf = buf.Slice(2);

        if (!typeId.TryFormat(buf, out var written))
            throw new Exception();

        buf = buf.Slice(written);

        buf[0] = ')';
        buf[1] = ' ';
        buf = buf.Slice(2);

        refs.CopyTo(buf);
        buf = buf.Slice(refs.Length);

        if (!_refs[typeId].TryFormat(buf, out written))
            throw new Exception();

        buf = buf.Slice(written);

        _writer.WriteLine(buffer.Slice(0, 512 - buf.Length));
    }

    // public void DumpTypes(bool includeEntities = false)
    // {

    // }

    // private int DumpType(int typeId, int indent = 0, int[]? heapCheck = null, int offset = 0)
    // {
    //     ref var typeRef = ref _db._types[typeId];
    //     var sym = typeRef.Symbol;
    //     var symbol = _db._symbols[sym];

    //     return 0;
    // }

    public void Dispose()
    {
        _writer.Dispose();
    }
}
