using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork;
using EntityWork.Utilities;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class UnsignedIntTypeDefinitionFactory<T>
    : Source2EntityTypeDefinitionFactory
    where T : unmanaged
{
    private readonly UnsignedIntTypeDefinition _definition;

    public UnsignedIntTypeDefinitionFactory(Source2EntityFlags flags = Source2EntityFlags.None)
    {
        _definition = new(flags);
    }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
    {
        return _definition;
    }

    internal sealed class UnsignedIntTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        internal readonly static EntityFieldDecoder InvokeDelegate32 = Invoke32;
        internal readonly static EntityFieldDecoder InvokeDelegate64 = Invoke64;

        private readonly EntityFieldDecoder _delly;
        private readonly string _typeName;
        private readonly Source2EntityFlags _flags;

        public UnsignedIntTypeDefinition()
            : this(Source2EntityFlags.None)
        {
        }

        public UnsignedIntTypeDefinition(Source2EntityFlags flags)
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.SByte:
                    _delly = InvokeDelegate32;
                    _typeName = "int8";
                    break;
                case TypeCode.Byte:
                    _delly = InvokeDelegate32;
                    _typeName = "uint8";
                    break;
                case TypeCode.Int16:
                    _delly = InvokeDelegate32;
                    _typeName = "int16";
                    break;
                case TypeCode.UInt16:
                    _delly = InvokeDelegate32;
                    _typeName = "uint16";
                    break;
                case TypeCode.Int32:
                    _delly = InvokeDelegate32;
                    _typeName = "int32";
                    break;
                case TypeCode.UInt32:
                    _delly = InvokeDelegate32;
                    _typeName = "uint32";
                    break;
                case TypeCode.Int64:
                    _delly = InvokeDelegate64;
                    _typeName = "int64";
                    break;
                case TypeCode.UInt64:
                    _delly = InvokeDelegate64;
                    _typeName = "uint64";
                    break;
                default:
                    throw new Exception(); // TODO Specify
            }

            _flags = flags;
        }

        public override string TypeName
        {
            get
            {
                return _typeName;
            }
        }

        private static int Invoke32(ref AlignedLsBitReader reader, in EntityWriter writer)
        {
            var read = reader.ReadVarUInt32(out var bitCount);

            //Debug.Assert((read & ~(~0U >> -(Unsafe.SizeOf<T>() * 8))) == 0);
            Debug.Assert(bitCount <= Unsafe.SizeOf<T>() * 8);

            if (writer.IsSkipped)
            {
                goto Exit;
            }

            MemoryMarshal.Write(writer.Dereference(), in Unsafe.As<uint, T>(ref read));

        Exit:
            return bitCount;
        }

        private static int Invoke64(ref AlignedLsBitReader reader, in EntityWriter writer)
        {
            var read = reader.ReadVarUInt64(out var bitCount);

            //Debug.Assert((read & ~(~0U >> -(Unsafe.SizeOf<T>() * 8))) == 0);
            Debug.Assert(bitCount <= Unsafe.SizeOf<T>() * 8);

            if (writer.IsSkipped)
            {
                goto Exit;
            }

            MemoryMarshal.Write(writer.Dereference(), in Unsafe.As<ulong, T>(ref read));

        Exit:
            return bitCount;
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sizeOf = Unsafe.SizeOf<T>();
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, _flags, Type.GetTypeCode(typeof(T)), sizeOf, 0, 0);

            return sizeOf;
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }
    }
}
