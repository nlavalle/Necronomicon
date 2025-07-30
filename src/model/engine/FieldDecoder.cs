using necronomicon.processor;

namespace necronomicon.model.engine;

public delegate object FieldDecoder(BitReaderWrapper reader);
public delegate FieldDecoder FieldFactory(Field field);

public static class FieldDecoders
{
    #region FieldDecoders
    public static FieldDecoder VectorNormalDecoder = reader =>
    {
        return reader.Read3BitNormal();
    };

    public static FieldDecoder Fixed64Decoder = reader =>
    {
        return reader.Reader.ReadUInt64LSB(64);
    };

    public static FieldDecoder HandleDecoder = reader =>
    {
        return reader.ReadVarUInt32();
    };

    public static FieldDecoder BooleanDecoder = reader =>
    {
        return reader.Reader.ReadBitLSB();
    };

    public static FieldDecoder StringDecoder = reader =>
    {
        return reader.ReadString();
    };

    public static FieldDecoder DefaultDecoder = reader =>
    {
        return reader.ReadVarUInt32();
    };

    public static FieldDecoder SignedDecoder = reader =>
    {
        return reader.ReadVarInt32();
    };

    public static FieldDecoder FloatCoordDecoder = reader =>
    {
        return reader.ReadCoord();
    };

    public static FieldDecoder NoScaleDecoder = reader =>
    {
        uint bits = reader.Reader.ReadUInt32LSB(32);
        return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
    };

    public static FieldDecoder RuneTimeDecoder = reader =>
    {
        uint bits = reader.Reader.ReadUInt32LSB(4);
        return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
    };

    public static FieldDecoder SimulationTimeDecoder = reader =>
    {
        uint value = reader.ReadVarUInt32();
        return value * (1f / 30f);
    };

    public static FieldDecoder Vector2Decoder = reader =>
    {
        return new float[] {
            BitConverter.ToSingle(BitConverter.GetBytes(reader.Reader.ReadUInt32LSB(32)), 0),
            BitConverter.ToSingle(BitConverter.GetBytes(reader.Reader.ReadUInt32LSB(32)), 0)
        };
    };

    public static FieldDecoder UnsignedDecoder = reader =>
    {
        return (ulong)reader.ReadVarUInt32();
    };

    public static FieldDecoder Unsigned64Decoder = reader =>
    {
        return reader.ReadVarUInt64();
    };

    public static FieldDecoder ComponentDecoder = reader =>
    {
        return reader.Reader.ReadBitLSB();
    };


    #endregion

    #region FieldFactories
    public static FieldFactory UnsignedFactory = field =>
    {
        return UnsignedDecoder;
    };

    public static FieldFactory Unsigned64Factory = field =>
    {
        switch (field.Encoder)
        {
            case "fixed64":
                return Fixed64Decoder;
        }
        return Unsigned64Decoder;
    };

    public static FieldFactory QuantizedFactory = field =>
    {
        var quantizedFloatDecoder = QuantizedFloatDecoder.New(field.BitCount, field.EncodeFlags, field.LowValue, field.HighValue);
        return reader => quantizedFloatDecoder.Decode(reader);
    };

    public static FieldFactory FloatFactory = field =>
    {
        switch (field.Encoder)
        {
            case "coord":
                return FloatCoordDecoder;
            case "simtime":
                return SimulationTimeDecoder;
            case "runetime":
                return RuneTimeDecoder;
        }

        if (field.BitCount == null || field.BitCount <= 0 || field.BitCount >= 32)
        {
            return NoScaleDecoder;
        }

        return QuantizedFactory(field);
    };

    public static FieldFactory VectorFactory(int n)
    {
        return field =>
        {
            if (n == 3 && field.Encoder == "normal")
            {
                return VectorNormalDecoder;
            }

            var floatDecoder = FloatFactory(field);

            return reader =>
            {
                var components = new float[n];
                for (int i = 0; i < n; i++)
                {
                    components[i] = (float)floatDecoder(reader);
                }

                return components;
            };
        };
    }

    public static FieldFactory QAngleFactory = field =>
    {
        if (field.Encoder == "qangle_pitch_yaw")
        {
            int n = field.BitCount ?? 0;
            return reader => new float[]{
                reader.ReadAngle(n),
                reader.ReadAngle(n),
                0.0f
            };
        }

        if (field.BitCount.HasValue && field.BitCount.Value != 0)
        {
            int n = field.BitCount.Value;
            return reader => new float[]
            {
                reader.ReadAngle(n),
                reader.ReadAngle(n),
                reader.ReadAngle(n)
            };
        }

        return reader =>
        {
            float[] ret = new float[3];
            bool rX = reader.Reader.ReadBitLSB();
            bool rY = reader.Reader.ReadBitLSB();
            bool rZ = reader.Reader.ReadBitLSB();

            if (rX) ret[0] = reader.ReadCoord();
            if (rY) ret[1] = reader.ReadCoord();
            if (rZ) ret[2] = reader.ReadCoord();

            return ret;
        };
    };
    #endregion

    public static readonly Dictionary<string, FieldFactory> FieldTypeFactories = new()
    {
        ["float32"] = FloatFactory,
        ["CNetworkedQuantizedFloat"] = QuantizedFactory,
        ["Vector"] = VectorFactory(3),
        ["Vector2D"] = VectorFactory(2),
        ["Vector4D"] = VectorFactory(4),
        ["uint64"] = Unsigned64Factory,
        ["QAngle"] = QAngleFactory,
        ["CHandle"] = UnsignedFactory,
        ["CStrongHandle"] = Unsigned64Factory,
        ["CEntityHandle"] = UnsignedFactory,
    };


    public static readonly Dictionary<string, FieldDecoder> FieldTypeDecoders = new()
    {
        ["bool"] = BooleanDecoder,
        ["char"] = StringDecoder,
        ["color32"] = UnsignedDecoder,
        ["int16"] = SignedDecoder,
        ["int32"] = SignedDecoder,
        ["int64"] = SignedDecoder,
        ["int8"] = SignedDecoder,
        ["uint16"] = UnsignedDecoder,
        ["uint32"] = UnsignedDecoder,
        ["uint8"] = UnsignedDecoder,

        ["GameTime_t"] = NoScaleDecoder,
        ["HeroFacetKey_t"] = Unsigned64Decoder,
        ["BloodType"] = UnsignedDecoder,

        ["CBodyComponent"] = ComponentDecoder,
        ["CGameSceneNodeHandle"] = UnsignedDecoder,
        ["Color"] = UnsignedDecoder,
        ["CPhysicsComponent"] = ComponentDecoder,
        ["CRenderComponent"] = ComponentDecoder,
        ["CUtlString"] = StringDecoder,
        ["CUtlStringToken"] = UnsignedDecoder,
        ["CUtlSymbolLarge"] = StringDecoder,
    };

    public static FieldDecoder FindDecoder(Field field)
    {
        if (FieldTypeFactories.TryGetValue(field.FieldType.BaseType, out var factory))
        {
            return factory(field);
        }

        if (FieldTypeDecoders.TryGetValue(field.VarName, out var nameDecoder))
        {
            return nameDecoder;
        }

        if (FieldTypeDecoders.TryGetValue(field.FieldType.BaseType, out var typeDecoder))
        {
            return typeDecoder;
        }

        return DefaultDecoder;
    }

    public static FieldDecoder FindDecoderByBaseType(string baseType)
    {
        if (FieldTypeDecoders.TryGetValue(baseType, out var decoder))
        {
            return decoder;
        }

        return DefaultDecoder;
    }
}