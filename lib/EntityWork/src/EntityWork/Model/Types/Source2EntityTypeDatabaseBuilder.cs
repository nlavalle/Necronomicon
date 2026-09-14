using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

public abstract class Source2EntityDatabaseBuilder
{
    private int _fieldCount = 1;
    private int _typeCount = 1;
    private int _entityOffset = 1;
    private int _entityMax = 0;

    private Source2SerializerDefinition[]? _placementRoots;

    private readonly Dictionary<string, Source2EntityTypeDefinitionFactory> _typeFactories;
    private readonly Dictionary<string, Source2EntityTypeDefinitionFactory>.AlternateLookup<ReadOnlySpan<char>> _typeLookup;
    private readonly Dictionary<string, TemplateTypeDefinitionFactory> _templateFactories;
    private readonly Dictionary<string, TemplateTypeDefinitionFactory>.AlternateLookup<ReadOnlySpan<char>> _templateLookup;
    private readonly FallbackTypeDefinitionFactory _fallbackFactory;

    private readonly Source2FieldDefinition?[] _fields;
    private readonly Dictionary<string, int> _reverseSymbol;
    private readonly Source2SerializerDefinition?[]?[] _serializerLookup;

    public Source2EntityInclusionFlags _flags;

    private Source2EntityTypeDatabase.Type[]? _typeOutput;
    private int _typeIncrement;
    private Source2EntityTypeDatabase.Field[]? _fieldOutput;
    private int _fieldIncrement;
    private Dictionary<string, int>? _symbolIntermediate;
    private int _symbolIncrement;
    private Dictionary<int, int>? _symbolIntermediateLow;
    private int _symbolIncrementLow;

    public CSVCMsg_FlattenedSerializer Serializer { get; }

    internal Source2EntityDatabaseBuilder(CSVCMsg_FlattenedSerializer serializer, Dictionary<string, Source2EntityTypeDefinitionFactory> typeFactories, Dictionary<string, TemplateTypeDefinitionFactory> templateFactories)
    {
        Serializer = serializer;
        _typeFactories = typeFactories;
        if (!typeFactories.TryGetAlternateLookup(out _typeLookup))
            throw new Exception();

        _templateFactories = templateFactories;
        if (!templateFactories.TryGetAlternateLookup(out _templateLookup))
            throw new Exception();

        _fallbackFactory = new FallbackTypeDefinitionFactory();
            

        _fields = new Source2FieldDefinition?[serializer.Fields.Count];
        var symbols = serializer.Symbols;
        var reverseSymbol = new Dictionary<string, int>(symbols.Count);

        for (int i = 0; i < symbols.Count; i++)
        {
            reverseSymbol.Add(symbols[i], i);
        }

        _reverseSymbol = reverseSymbol;

        var serializerLookup = new Source2SerializerDefinition?[]?[serializer.Serializers.Count];

        for (int i = 0; i < serializer.Serializers.Count; i++)
        {
            var ser = serializer.Serializers[i];
            var serDef = new Source2SerializerDefinition(this, i);
            
            var symId = ser.SerializerNameSym;
            var serVers = serializerLookup[symId];
            var serVer = ser.SerializerVersion;
            if (serVers is null)
            {
                if (serVer != 0)
                {
                    if (serVer < 4)
                    {
                        serVers = new Source2SerializerDefinition?[4];
                    }
                    else
                    {
                        serVers = new Source2SerializerDefinition?[(int)BitOperations.RoundUpToPowerOf2((uint)serVer + 1)];
                    }
                }
                else
                {
                    serVers = new Source2SerializerDefinition?[1];
                }

                serializerLookup[symId] = serVers;
            }
            else
            {
                if (serVer >= serVers.Length)
                {
                    var oldVers = serVers;
                    if (serVer < 4)
                    {
                        serVers = new Source2SerializerDefinition?[4];
                    }
                    else
                    {
                        serVers = new Source2SerializerDefinition?[(int)BitOperations.RoundUpToPowerOf2((uint)serVer + 1)];
                    }

                    oldVers.AsSpan().CopyTo(serVers);
                    serializerLookup[symId] = serVers;
                }
            }

            serVers[serVer] = serDef;
        }

        _serializerLookup = serializerLookup;
    }

    public void ReservationPhase(CDemoClassInfo classInfo)
    {
        int minId, maxId;
        int offset = 1;

        var count = classInfo.Classes.Count;
        if (count > 0)
        {
            var roots = new Source2SerializerDefinition[count];
            var id = classInfo.Classes[0].ClassId;
            offset -= id;
            minId = id;
            maxId = id - 1;

            for (int i = 0; i < classInfo.Classes.Count; i++)
            {
                var cls = classInfo.Classes[i];
                id = cls.ClassId;

                Debug.Assert(cls.HasNetworkName);
                Debug.Assert(!cls.HasTableName);

                if (id < minId || id <= maxId)
                    throw new UnreachableException();

                maxId = id;

                var serDef = GetSerializerDefinitionByNetworkName(cls.NetworkName);
                serDef.Reserve(this, serDef, id + offset);
                roots[i] = serDef;
            }

            _entityOffset = offset;
            _entityMax = maxId + 1;
            _placementRoots = roots;
        }


        var range = offset;
    }

    internal Source2SerializerDefinition GetSerializerDefinitionBySymbol(int symbolId, int version = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)symbolId, (uint)_serializerLookup.Length, nameof(symbolId));

        var symbolSers = _serializerLookup[symbolId];
        if (symbolSers is null)
            throw new ArgumentOutOfRangeException(nameof(symbolId));

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)version, (uint)symbolSers.Length, nameof(version));

        var serDef = symbolSers[version];
        if (serDef is null)
            throw new ArgumentOutOfRangeException(nameof(version));

        return serDef;
    }

    internal Source2SerializerDefinition GetSerializerDefinitionByNetworkName(string name)
    {
        if (!_reverseSymbol.TryGetValue(name, out var symbolId))
            throw new ArgumentException("Invalid network name provided.", nameof(name));

        return GetSerializerDefinitionBySymbol(symbolId);
    }

    public void ReserveFieldCount(int count)
    {
        _fieldCount += count;
    }

    internal void ReserveType()
    {
        _typeCount++;
    }

    internal void ReserveTypeAndFieldCount(int typeCount, int fieldCount)
    {
        _typeCount += typeCount;
        _fieldCount += fieldCount;
    }

    internal void ReserveTypeAndFieldCount(int fieldCount)
    {
        _typeCount++;
        _fieldCount += fieldCount;
    }

    internal Source2EntityTypeDatabase.HeapOffset[]? GetHeapOffsetsForPlacedType(int typeId)
    {
        var types = _typeOutput;
        Debug.Assert(types is not null);

        return types[typeId].HeapOffsets;
    }

    public Source2EntityTypeDatabase Build()
    {
        var types = new Source2EntityTypeDatabase.Type[_typeCount];
        _typeOutput = types;
        _typeIncrement = _entityMax + 1;

        var fields = new Source2EntityTypeDatabase.Field[_fieldCount];
        _fieldOutput = fields;
        _fieldIncrement = 1;

        var symbolIntermediate = new Dictionary<string, int>(_fieldCount + _typeCount);
        symbolIntermediate.Add("NULL", 0);
        _symbolIntermediate = symbolIntermediate;
        _symbolIncrement = 1;

        var symbolIntermediateLow = new Dictionary<int, int>(_fieldCount);
        symbolIntermediateLow.Add(0, 0);
        _symbolIntermediateLow = symbolIntermediateLow;
        _symbolIncrementLow = 1;

        var roots = _placementRoots ?? throw new InvalidOperationException();

        var flags = _flags;

        foreach (var guy in roots)
        {
            guy.GetOrPlace(this, flags);
        }

        var symbols = new string[symbolIntermediate.Count];
        foreach (var symbol in symbolIntermediate)
        {
            symbols[symbol.Value] = symbol.Key;
        }

        var symbolsLow = new int[symbolIntermediateLow.Count];
        foreach (var symLow in symbolIntermediateLow)
        {
            symbolsLow[symLow.Value] = symLow.Key;
        }

        return new(types, fields, symbols, symbolsLow, _entityMax);
    }

    internal Span<Source2EntityTypeDatabase.Field> AllocateFields(int count, out int index)
    {
        var fields = _fieldOutput;
        Debug.Assert(fields is not null);

        var placement = _fieldIncrement;
        var update = placement + count;
        Debug.Assert(update <= fields.Length);

        _fieldIncrement = update;

        index = placement;
        return MemoryMarshal.CreateSpan(ref fields[placement], count);
    }

    internal Span<Source2EntityTypeDatabase.Field> GetFields(int index, int count)
    {
        var fields = _fieldOutput;
        Debug.Assert(fields is not null);

        Debug.Assert(index + count <= fields.Length);

        return MemoryMarshal.CreateSpan(ref fields[index], count);
    }

    internal Span<Source2EntityTypeDatabase.Type> AllocateType(out int index)
    {
        var types = _typeOutput;
        Debug.Assert(types is not null);

        var placement = _typeIncrement;
        var update = placement + 1;
        Debug.Assert(update <= types.Length);

        _typeIncrement = update;

        index = placement;
        return MemoryMarshal.CreateSpan(ref types[placement], 1);
    }

    internal Span<Source2EntityTypeDatabase.Type> GetType(int index)
    {
        var types = _typeOutput;
        Debug.Assert(types is not null);

        Debug.Assert(index < types.Length);

        return MemoryMarshal.CreateSpan(ref types[index], 1);
    }

    internal int GetOrPlaceSymbol(string symbol)
    {
        var symbols = _symbolIntermediate;
        Debug.Assert(symbols is not null);

        if (!symbols.TryGetValue(symbol, out var placement))
        {
            placement = _symbolIncrement;
            symbols.Add(symbol, placement);
            _symbolIncrement = placement + 1;
        }

        return placement;
    }

    internal ushort GetOrPlaceSymbolLow(string symbol)
    {
        var symbolId = GetOrPlaceSymbol(symbol);

        var symbols = _symbolIntermediateLow;
        Debug.Assert(symbols is not null);

        if (!symbols.TryGetValue(symbolId, out var placement))
        {
            placement = _symbolIncrementLow;
            Debug.Assert(placement <= ushort.MaxValue);

            symbols.Add(symbolId, placement);
            _symbolIncrementLow = placement + 1;
        }

        return (ushort)placement;
    }

    internal Source2FieldDefinition GetFieldDefinition(int index)
    {
        var field = _fields[index];
        if (field is null)
        {
            field = new(index);
            _fields[index] = field;
        }

        return field;
    }

    internal Source2EntityTypeDefinitionBase CreateParsedType(ProtoFlattenedSerializerField_t? field, string typeSym)
    {
        return CreateParsedType(field, typeSym, 0, typeSym.Length);
    }

    internal Source2EntityTypeDefinitionBase CreateParsedType(ProtoFlattenedSerializerField_t? field, string typeSym, int start, int length)
    {
        start = DrainLeftWhiteSpace(typeSym, start, length);
        length = DrainRightWhiteSpace(typeSym, start, length);

        var lastIndex = start + length - 1;

        switch (typeSym[lastIndex])
        {
            case '*':
                return _fallbackFactory.GetTypeDefinition(this, field, true);
            case ']':
                {
                    var index = typeSym.AsSpan(start, length).LastIndexOf('[');

                    var innerStart = start;
                    var innerLength = DrainRightWhiteSpace(typeSym, innerStart, index);

                    var lengthStart = index + 1;
                    var lengthLength = lastIndex - lengthStart;
                    lengthStart = DrainLeftWhiteSpace(typeSym, lengthStart, lengthLength);
                    lengthLength = DrainRightWhiteSpace(typeSym, lengthStart, lengthLength);

                    return new InlineArrayTypeDefinition(field, typeSym, innerStart, innerLength, lengthStart, lengthLength);
                }
            case '>':
                {
                    var index = typeSym.AsSpan(start, length).IndexOf('<');

                    var outerStart = start;
                    var outerLength = DrainRightWhiteSpace(typeSym, outerStart, index);

                    var span = typeSym.AsSpan(outerStart, outerLength);

                    if (_templateLookup.TryGetValue(span, out var templateGuy))
                    {
                        var innerStart = start + index + 1;
                        var innerLength = lastIndex - innerStart;
                        innerStart = DrainLeftWhiteSpace(typeSym, innerStart, innerLength);
                        innerLength = lastIndex - innerStart;
                        innerLength = DrainRightWhiteSpace(typeSym, innerStart, innerLength);

                        return templateGuy.GetTypeDefinition(this, field, typeSym, outerStart, outerLength, innerStart, innerLength);
                    }

                    if (_typeLookup.TryGetValue(span, out var typeGuy))
                    {
                        return typeGuy.GetTypeDefinition(this, field);
                    }
                }
                break;
            default:
                {
                    if (_typeLookup.TryGetValue(typeSym.AsSpan(start, length), out var typeFactory))
                    {
                        return typeFactory.GetTypeDefinition(this, field);
                    }
                }
                break;
        }

        if (field is not null)
        {
            if (field.HasFieldSerializerNameSym)
            {
                return GetSerializerDefinitionBySymbol(field.FieldSerializerNameSym, field.FieldSerializerVersion);
            }

            if (field.HasVarEncoderSym && _typeFactories.TryGetValue(Serializer.Symbols[field.VarEncoderSym], out var typeFactory))
            {
                return typeFactory.GetTypeDefinition(this, field);
            }
        }

        return _fallbackFactory.GetTypeDefinition(this, field, false);
    }

    private static int DrainLeftWhiteSpace(string value, int start, int length)
    {
        Debug.Assert((ulong)(uint)start + (ulong)(uint)length <= (uint)value.Length);

        while (length-- > 0 && char.IsWhiteSpace(value[start]))
            start++;

        return start;
    }

    private static int DrainRightWhiteSpace(string value, int start, int length)
    {
        Debug.Assert((ulong)(uint)start + (ulong)(uint)length <= (uint)value.Length);
        var pos = start + length;

        while (--pos >= start && char.IsWhiteSpace(value[pos])) ;

        return pos - start + 1;
    }

    internal int GetConstValue(ReadOnlySpan<char> typeSym)
    {
        return 128;
    }

    internal static Source2EntityInclusionFlags CalculateFlagsForThis(Source2EntityInclusionFlags fromParent, Source2EntityInclusionFlags fromThis)
    {
        uint parentCalc = (uint)fromParent;
        uint thisCalc = (uint)fromThis;
        uint forThis = (parentCalc & ~(thisCalc >> 2)) | (thisCalc & 3U);

        return (Source2EntityInclusionFlags)forThis;
    }

    internal static Source2EntityFlags ConvertFlagsForField(Source2EntityInclusionFlags forThis)
    {
        Source2EntityFlags forCalc = Source2EntityFlags.None;
        if ((forThis & Source2EntityInclusionFlags.IncludeField) == 0)
            forCalc = Source2EntityFlags.Exclude;

        return forCalc;
    }

    internal static Source2EntityFlags ConvertFlagsForType(Source2EntityInclusionFlags forThis, Source2EntityFlags mixin)
    {
        Source2EntityFlags forCalc = Source2EntityFlags.None;
        if ((forThis & Source2EntityInclusionFlags.IncludeSet) == 0)
            forCalc = Source2EntityFlags.Exclude;

        return forCalc | (mixin & ~Source2EntityFlags.Exclude);
    }

    internal static Source2EntityInclusionFlags CalculateFlagsForChild(Source2EntityInclusionFlags forThis, Source2EntityInclusionFlags fromThis)
    {
        uint forCalc = (uint)forThis;
        uint fromCalc = (uint)fromThis;
        uint forChild = forCalc & ~(fromCalc >> 4);

        return (Source2EntityInclusionFlags)forChild;
    }
}
