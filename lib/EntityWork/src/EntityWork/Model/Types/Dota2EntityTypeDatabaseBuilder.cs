using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

public sealed class Dota2EntityDatabaseBuilder
    : Source2EntityDatabaseBuilder
{
    private static Dictionary<string, Source2EntityTypeDefinitionFactory> CreateTypeFactories()
    {
        var uint8Factory = new UnsignedIntTypeDefinitionFactory<byte>();
        var uint16Factory = new UnsignedIntTypeDefinitionFactory<ushort>();
        var uint32Factory = new UnsignedIntTypeDefinitionFactory<uint>();
        var uint64Factory = new UInt64TypeDefinitionFactory();
        var int8Factory = new SignedIntTypeDefinitionFactory<sbyte>();
        var int16Factory = new SignedIntTypeDefinitionFactory<short>();
        var int32Factory = new SignedIntTypeDefinitionFactory<int>();
        var int64Factory = new SignedIntTypeDefinitionFactory<long>();
        var charFactory = new CharTypeDefinitionFactory();
        var stringFactory = new StringTypeDefinitionFactory(charFactory);
        var floatFactory = new FloatTypeDefinitionFactory(uint32Factory);
        var qangleFactory = new QAngleTypeDefinitionFactory(floatFactory);
        var vectorFactory = new VectorTypeDefinitionFactoryFactory(floatFactory);
        var vector2Factory = vectorFactory.CreateFactory(2);
        var vector3Factory = vectorFactory.CreateFactory(3);
        var vector4Factory = vectorFactory.CreateFactory(4);

        return new()
        {
            ["bool"] = new BoolTypeDefinitionFactory(),

            ["uint8"] = uint8Factory,
            ["uint16"] = uint16Factory,
            ["uint32"] = uint32Factory,
            ["uint64"] = uint64Factory,

            ["int8"] = int8Factory,
            ["int16"] = int16Factory,
            ["int32"] = int32Factory,
            ["int64"] = int64Factory,

            ["CUtlSymbolLarge"] = stringFactory,
            ["CUtlString"] = stringFactory,
            ["char"] = charFactory,
            // ["CUtlStringToken"] = UnsignedDecoder,

            // Handles
            ["CHandle"] = uint32Factory,
            ["CEntityHandle"] = uint32Factory,
            ["CGameSceneNodeHandle"] = uint32Factory,
            ["CBaseVRHandAttachmentHandle"] = uint32Factory,
            ["CStrongHandle"] = uint64Factory,

            // Colors
            ["Color"] = uint32Factory,
            ["color32"] = uint32Factory,

            // Specials
            // ["BloodType"] = UnsignedDecoder,
            // ["GameTime_t"] = NoScaleDecoder,
            // ["HeroFacetKey_t"] = Unsigned64Decoder,
            // ["HeroID_t"] = SignedDecoder,

            // Angles
            ["QAngle"] = qangleFactory,

            // Still unknown baseTypes
            // CGlobalSymbol
            // CTransform

            // Floats
            ["float32"] = floatFactory,
            ["CNetworkedQuantizedFloat"] = floatFactory,

            // Vectors
            ["Vector"] = vector3Factory,
            ["Vector2D"] = vector2Factory,
            ["Vector4D"] = vector4Factory,
            ["Quaternion"] = vector4Factory,
        };

    }

    private static Dictionary<string, TemplateTypeDefinitionFactory> CreateTemplateFactories()
    {
        var dynamicArrayFactory = new DynamicArrayTemplateFactory();
        
        return new()
        {
            ["CUtlVector"] = dynamicArrayFactory,
            ["CNetworkUtlVectorBase"] = dynamicArrayFactory,
            ["CUtlVectorEmbeddedNetworkVar"] = dynamicArrayFactory,
        };
    }

    public Dota2EntityDatabaseBuilder(CSVCMsg_FlattenedSerializer serializer)
        : base(serializer, CreateTypeFactories(), CreateTemplateFactories())
    {
    }
}

