using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork;
using EntityWork.Utilities;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed partial class QAngleTypeDefinitionFactory
    : Source2EntityTypeDefinitionFactory
{
    // TODO Create static cache for "Create" decoders
    private static readonly EntityFieldDecoder _precise = InvokePrecise;
    private static readonly EntityFieldDecoder _fixed = InvokeFixed;
    private static readonly EntityFieldDecoder _zeroBitCount = InvokeZeroBitCount;

    private readonly DimensionalVectorFieldEnclosure _inner;
    private readonly Dictionary<CacheKey, QAngleTypeDefinition> _cache;

    private readonly record struct CacheKey(int EncoderSym, int BitCount);

    public QAngleTypeDefinitionFactory(FloatTypeDefinitionFactory floatFactory)
    {
        var inner = new DimensionalVectorFieldEnclosure(floatFactory.ReadOnlyDefinition, 3);
        _inner = inner;

        _cache = new Dictionary<CacheKey, QAngleTypeDefinition>();
    }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
    {
        ArgumentNullException.ThrowIfNull(field, nameof(field));

        // If this assertion ever fails, see the commented code to understand the assumptions
        Debug.Assert(field.HasVarEncoderSym, "Failed assumption, see comments");

        int encoderSym;
        // if (field.HasVarEncoderSym)
        // {
            encoderSym = field.VarEncoderSym;
        // }
        // else
        // {
        //     encoderSym = -1;
        // }


        var bitCount = field.BitCount;
        var key = new CacheKey(encoderSym, bitCount);

        if (!_cache.TryGetValue(key, out var value))
        {
            string encoder;

            // if (encoderSym == -1)
            // {
            //     encoder = "qangle";
            // }
            // else
            // {
                encoder = builder.Serializer.Symbols[encoderSym];
            // }

            if (encoder == "qangle_pitch_yaw")
            {
                value = new QAngleTypeDefinition(CreatePitchYaw(bitCount), _inner);
            }
            else if (encoder == "qangle_precise")
            {
                value = new QAngleTypeDefinition(_precise, _inner);
            }
            else // if (encoder == "qangle")
            {
                Debug.Assert(encoder == "qangle", "Failed assumption");

                if (bitCount == 0)
                {
                    value = new QAngleTypeDefinition(_zeroBitCount, _inner);
                }
                else if (bitCount < 32)
                {
                    value = new QAngleTypeDefinition(CreateQAngle(bitCount), _inner);
                }
                else
                {
                    Debug.Assert(bitCount == 32);
                    value = new QAngleTypeDefinition(_fixed, _inner);
                }
            }

            _cache.Add(key, value);
        }

        return value;
    }

    internal static EntityFieldDecoder CreatePitchYaw(int bitCount)
    {
        var mult = 360f / (1U << bitCount);
        var bitsRead = bitCount * 2;

        return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
        {
            if (writer.IsSkipped)
            {
                reader.Advance(bitsRead);

                goto Exit;
            }

            var span = writer.Dereference();

            var read = reader.ReadUnsigned<uint>(bitCount);
            MemoryMarshal.Write(span, read * mult);

            read = reader.ReadUnsigned<uint>(bitCount);
            MemoryMarshal.Write(span.Slice(sizeof(float)), read * mult);

            span.Slice(sizeof(float) * 2).Clear();

        Exit:
            return bitsRead;
        };
    }

    internal static EntityFieldDecoder CreateQAngle(int bitCount)
    {
        var mult = 360f / (1U << bitCount);
        var bitsRead = bitCount * 3;

        return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
        {
            if (writer.IsSkipped)
            {
                reader.Advance(bitsRead);

                goto Exit;
            }

            var span = writer.Dereference();

            var read = reader.ReadUnsigned<uint>(bitCount);
            MemoryMarshal.Write(span, read * mult);

            read = reader.ReadUnsigned<uint>(bitCount);
            MemoryMarshal.Write(span.Slice(sizeof(float)), read * mult);

            read = reader.ReadUnsigned<uint>(bitCount);
            MemoryMarshal.Write(span.Slice(sizeof(float) * 2), read * mult);

        Exit:
            return bitsRead;
        };
    }

    internal static int InvokeFixed(ref AlignedLsBitReader reader, in EntityWriter writer)
    {
        const int bitsRead = 32 * 3;

        if (writer.IsSkipped)
        {
            reader.Advance(bitsRead);

            goto Exit;
        }

        var span = writer.Dereference();

        var read = reader.ReadUnsigned<uint>(32);
        MemoryMarshal.Write(span, in read);

        read = reader.ReadUnsigned<uint>(32);
        MemoryMarshal.Write(span.Slice(sizeof(float)), in read);

        read = reader.ReadUnsigned<uint>(32);
        MemoryMarshal.Write(span.Slice(sizeof(float) * 2), in read);

    Exit:
        return bitsRead;
    }

    internal static int InvokePrecise(ref AlignedLsBitReader reader, in EntityWriter writer)
    {
        const float mult = 360f / (1U << 20);

        var flags = reader.ReadUnsigned<int>(3);
        var bitsRead = 3;

        if (writer.IsSkipped)
        {
            var skip = int.PopCount(flags) * 20;
            reader.Advance(skip);
            bitsRead += skip;
            
            goto Exit;
        }

        var span = writer.Dereference();

        if ((flags & 1) != 0)
        {
            var read = reader.ReadUnsigned<uint>(20);
            MemoryMarshal.Write(span, read * mult);
            bitsRead += 20;
        }
        if ((flags & 2) != 0)
        {
            var read = reader.ReadUnsigned<uint>(20);
            MemoryMarshal.Write(span.Slice(sizeof(float)), read * mult);
            bitsRead += 20;
        }
        if ((flags & 4) != 0)
        {
            var read = reader.ReadUnsigned<uint>(20);
            MemoryMarshal.Write(span.Slice(sizeof(float) * 2), read * mult);
            bitsRead += 20;
        }

    Exit:
        return bitsRead;
    }

    internal static int InvokeZeroBitCount(ref AlignedLsBitReader reader, in EntityWriter writer)
    {
        var flags = reader.ReadUnsigned<int>(3);
        var bitsRead = 3;

        if (writer.IsSkipped)
        {
            var count = int.PopCount(flags);

            for (int i = 0; i < count; i++)
            {
                bitsRead += reader.SkipCoord();
            }
            
            goto Exit;
        }

        var span = writer.Dereference();

        if ((flags & 1) != 0)
        {
            bitsRead += reader.ReadCoordInto(span);
        }
        else
        {
            span.Slice(0, sizeof(float)).Clear();
        }

        if ((flags & 2) != 0)
        {
            bitsRead += reader.ReadCoordInto(span.Slice(sizeof(float)));
        }
        else
        {
            span.Slice(sizeof(float), sizeof(float)).Clear();
        }

        if ((flags & 4) != 0)
        {
            bitsRead += reader.ReadCoordInto(span.Slice(sizeof(float) * 2));
        }
        else
        {
            span.Slice(sizeof(float) * 2, sizeof(float)).Clear();
        }

    Exit:
        return bitsRead;
    }

    public sealed class QAngleTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private const string OuterName = "QAngle";

        private readonly EntityFieldDecoder _delly;
        private readonly DimensionalVectorFieldEnclosure _inner;

        public override string TypeName => OuterName;
        public override int AlignOf => _inner.AlignOf;

        internal QAngleTypeDefinition(EntityFieldDecoder decoder, DimensionalVectorFieldEnclosure inner)
        {
            _delly = decoder;
            _inner = inner;
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            _inner.Reserve(builder, serDef);

            builder.ReserveType();
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sizeOf = Unsafe.SizeOf<Vector3>();

            var index = _inner.GetOrPlace(builder, Source2EntityInclusionFlags.None);

            var sym = builder.GetOrPlaceSymbol(OuterName);
            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, Source2EntityFlags.None, TypeCode.Object, sizeOf, index, 3);

            return sizeOf;
        }
    }
}
