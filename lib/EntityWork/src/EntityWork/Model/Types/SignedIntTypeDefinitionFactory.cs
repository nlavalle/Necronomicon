using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork;
using EntityWork.Utilities;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class SignedIntTypeDefinitionFactory<T>
    : Source2EntityTypeDefinitionFactory
    where T : unmanaged
{
    private readonly SignedIntTypeDefinition _definition = new();

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
    {
        return _definition;
    }

    internal sealed class SignedIntTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private readonly static EntityFieldDecoder _delly32 = Invoke32;
        private readonly static EntityFieldDecoder _delly64 = Invoke64;

        private readonly EntityFieldDecoder _delly;
        private readonly string _typeName;

        public SignedIntTypeDefinition()
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.SByte:
                    _delly = _delly32;
                    _typeName = "int8";
                    break;
                case TypeCode.Byte:
                    _delly = _delly32;
                    _typeName = "uint8";
                    break;
                case TypeCode.Int16:
                    _delly = _delly32;
                    _typeName = "int16";
                    break;
                case TypeCode.UInt16:
                    _delly = _delly32;
                    _typeName = "uint16";
                    break;
                case TypeCode.Int32:
                    _delly = _delly32;
                    _typeName = "int32";
                    break;
                case TypeCode.UInt32:
                    _delly = _delly32;
                    _typeName = "uint32";
                    break;
                case TypeCode.Int64:
                    _delly = _delly64;
                    _typeName = "int64";
                    break;
                case TypeCode.UInt64:
                    _delly = _delly64;
                    _typeName = "uint64";
                    break;
                default:
                    throw new Exception(); // TODO Specify
            }
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

            ZigZaggery.UnZigZagReference(ref read);

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

            ZigZaggery.UnZigZagReference(ref read);

            MemoryMarshal.Write(writer.Dereference(), in Unsafe.As<ulong, T>(ref read));

        Exit:
            return bitCount;
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sizeOf = Unsafe.SizeOf<T>();
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, Source2EntityFlags.None, Type.GetTypeCode(typeof(T)), sizeOf, 0, 0);

            return sizeOf;
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }
    }
}
