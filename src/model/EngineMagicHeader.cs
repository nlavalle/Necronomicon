namespace necronomicon.model;

public enum EngineMagicHeader : long
{
    UNKNOWN         = 0L,
    SOURCE_2        = 5783259935937868288L, // BinaryPrimitives.ReadInt64BigEndian("PBDEMS2\0"u8)
    DOTA_SOURCE_1   = 5783278631778602240L, // BinaryPrimitives.ReadInt64BigEndian("PBUFDEM\0"u8)
    CSGO_SOURCE_1   = 5209594137762680576L, // BinaryPrimitives.ReadInt64BigEndian("HL2DEMO\0"u8)
}
