using BitWork;

namespace EntityWork.Model.Types;

// This type should not implement Source2EntityTypeDefinitionBase, because it should not be possible for any type to contain it
internal sealed class NullTypeDefinition
{
    private const TypeCode NullTypeCode = TypeCode.Empty;

    private readonly static EntityFieldDecoder _delly = Invoke;
    public readonly static NullTypeDefinition Singleton = new();

    public string TypeName => "NULL";
    public int SizeOf => 0;

    private NullTypeDefinition() { }

    private static int Invoke(ref AlignedLsBitReader reader, in EntityWriter writer)
    {
        throw new NullReferenceException();
    }

    internal int GetOrPlace(Source2EntityDatabaseBuilder builder, Source2EntityInclusionFlags parentFlags)
    {
        var type = builder.GetType(0);
        var sym = builder.GetOrPlaceSymbol("NULL");
        type[0] = new(_delly, null, sym, Source2EntityFlags.None, NullTypeCode, 0, int.MaxValue, 0);

        var fields = builder.GetFields(0, 1);
        GenerateAutoField(sym, ref fields[0]);

        return 0;
    }

    private static void GenerateAutoField(int symbol, ref Source2EntityTypeDatabase.Field field)
    {
        field = new(symbol, 0, Source2EntityFlags.None, 0, int.MinValue);
    }

}
