using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EntityWork.Model.Types;

public readonly ref struct TypeMaterializer
{
    internal readonly Source2EntityTypeDatabase? TypeDb;
    private readonly ref Source2EntityTypeDatabase.Type _typeRef;
    internal readonly int TypeId;

    public TypeMaterializer(Source2EntityTypeDatabase typeDb, int typeId)
    {
        TypeDb = typeDb;
        TypeId = typeId;
        _typeRef = ref typeDb._types[typeId];
    }

    public EntityFieldDecoder? Decoder => !Unsafe.IsNullRef(in _typeRef) ? _typeRef.Decoder : null;
    public Source2EntityFlags Flags => !Unsafe.IsNullRef(in _typeRef) ? _typeRef.Flags : Source2EntityFlags.None;

    public bool IsArrayType => (Flags & Source2EntityFlags.Array) != 0;
    public bool IsHeapType => TypeId < 0;
}
