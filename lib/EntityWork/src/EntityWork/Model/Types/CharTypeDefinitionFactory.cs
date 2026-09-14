using System.Diagnostics;
using BitWork;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class CharTypeDefinitionFactory
    : Source2EntityTypeDefinitionFactory
{
    public readonly CharTypeDefinition _definition = new();

    public CharTypeDefinitionFactory() { }

    public override Source2EntityTypeDefinitionBase GetTypeDefinition(Source2EntityDatabaseBuilder builder, ProtoFlattenedSerializerField_t? field)
        => _definition;

    internal sealed class CharTypeDefinition
        : Source2EntityTypeDefinitionBase
    {
        private readonly static EntityFieldDecoder _delly = Invoke;
        private readonly static EntityFieldDecoder _dellyArray = InvokeArray;
        private readonly static EntityFieldDecoder _dellyHeap = InvokeHeap;

        public override string TypeName => "char";

        internal override EntityFieldDecoder? GetOuterDecoder(Source2EntityFlags flags)
        {
            return (flags & (Source2EntityFlags.Heap | Source2EntityFlags.Array)) switch
            {
                Source2EntityFlags.Heap | Source2EntityFlags.Array => _dellyHeap,
                Source2EntityFlags.Array => _dellyArray,
                _ => null,
            };
        }

        protected override void ReserveInternal(Source2EntityDatabaseBuilder builder, Source2SerializerDefinition? serDef)
        {
            builder.ReserveType();
        }

        internal override int PlaceInternal(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags, ref Source2EntityTypeDatabase.Type type, int typeId)
        {
            var sym = builder.GetOrPlaceSymbol(TypeName);

            type = new Source2EntityTypeDatabase.Type(_delly, null, sym, Source2EntityFlags.None, TypeCode.Byte, sizeof(byte), 0, 0);

            return sizeof(byte);
        }

        private static int Invoke(ref AlignedLsBitReader reader, in EntityWriter writer)
        {
            const int readSize = sizeof(byte) * 8;
            bool result;

            if (writer.IsSkipped)
            {
                result = reader.TryAdvance(readSize);
                goto Exit;
            }

            result = reader.TryReadUnsigned(readSize, out writer.Dereference()[0]);

        Exit:
            if (!result)
                throw new Exception();

            return readSize;
        }

        private static int InvokeArray(ref AlignedLsBitReader reader, in EntityWriter span)
        {
            int readSize;

            if (span.IsSkipped)
            {
                readSize = reader.GetByteCountUntilZero() * 8 + 8;
                reader.TryAdvance((uint)readSize);
                goto Exit;
            }

            if (!reader.TryCopyBytesUntilZero(span.Dereference(), out readSize))
                throw new Exception(); // TODO Specify: span too small

            readSize = readSize * 8 + 8;

        Exit:
            return readSize;
        }

        private static int InvokeHeap(ref AlignedLsBitReader reader, in EntityWriter writer)
        {
            int byteSize = reader.GetByteCountUntilZero();
            Debug.Assert(byteSize <= (int.MaxValue - 8) / 8);

            int bitSize = byteSize * 8 + 8;

            if (!writer.TryAllocate(byteSize, out var span))
            {
                reader.TryAdvance((uint)bitSize);
                goto Exit;
            }

            if (!reader.TryCopyTo(span))
                throw new UnreachableException("Heap should have preallocated span.");

            reader.TryAdvance(8);

        Exit:
            return bitSize;
        }
    }

}
