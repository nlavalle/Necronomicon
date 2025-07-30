using System.Diagnostics;
using necronomicon.processor;
using Snappier;
using Steam.Protos.Dota2;

namespace necronomicon.model.engine;

public class StringTables
{
    public Dictionary<int, StringTable> Tables = new Dictionary<int, StringTable>();
    public Dictionary<string, int> NameIndex = new Dictionary<string, int>();
}

public class SvcStringTable
{
    private readonly Necronomicon _parser;
    public SvcStringTable(Necronomicon parser)
    {
        _parser = parser;

        _parser.Callbacks.OnSvcCreateStringTable.Add(OnCSVCMsgCreateStringTable);
        _parser.Callbacks.OnSvcUpdateStringTable.Add(OnCSVCMsgUpdateStringTable);
    }

    public async Task OnCSVCMsgCreateStringTable(CSVCMsg_CreateStringTable createStringTable)
    {
        int stringTableIndex = _parser.StringTables.Tables.Count;
        StringTable newStringTable = new StringTable(stringTableIndex, createStringTable);

        byte[] messageBuffer;
        if (createStringTable.DataCompressed)
        {
            var compressedBuffer = createStringTable.StringData.ToArray();
            int uncompressedLength = Snappy.GetUncompressedLength(compressedBuffer);
            messageBuffer = new byte[uncompressedLength];
            Snappy.Decompress(compressedBuffer, messageBuffer);
        }
        else
        {
            messageBuffer = createStringTable.StringData.ToArray();
        }

        // Parse the items out of the string table data
        ParseStringTable(messageBuffer, createStringTable.NumEntries, newStringTable);

        // // Add the table to the parser state
        _parser.StringTables.Tables[newStringTable.Index] = newStringTable;
        _parser.StringTables.NameIndex[newStringTable.Name] = newStringTable.Index;

        // Apply the updates to baseline state
        if (newStringTable.Name == "instancebaseline")
        {
            _parser.UpdateInstanceBaseline();
        }

        // // Emit events for modifier table entry updates
        // if t.name == "ActiveModifiers" {
        // 	if err := p.emitModifierTableEvents(items); err != nil {
        // 		return err
        // 	}
        // }

        await Task.CompletedTask;
    }

    public async Task OnCSVCMsgUpdateStringTable(CSVCMsg_UpdateStringTable updateStringTable)
    {
        if (_parser.StringTables.Tables.TryGetValue(updateStringTable.TableId, out var stringTable))
            {
                ParseStringTable(updateStringTable.StringData.ToArray(), updateStringTable.NumChangedEntries, stringTable);

                // Apply the updates to baseline state
                if (stringTable.Name == "instancebaseline")
                {
                    _parser.UpdateInstanceBaseline();
                }
            }
        await Task.CompletedTask;
    }

    public void ParseStringTable(byte[] stringTableBuffer, int numEntries, StringTable stringTable)
    {
        BitReaderWrapper reader = new BitReaderWrapper(stringTableBuffer);

        // Start with an index of -1.
        // If the first item is at index 0 it will use a incr operation.
        var index = -1;

        if (stringTableBuffer.Length == 0)
        {
            return;
        }

        // Maintain a list of key history
        var keys = new List<string>(32);

        for (int i = 0; i < numEntries; i++)
        {
            byte[] value = Array.Empty<byte>();

            // Read a boolean to determine whether the operation is an increment or
            // has a fixed index position. A fixed index position of zero should be
            // the last data in the buffer, and indicates that all data has been read.
            if (reader.Reader.ReadBitLSB())
            {
                index++;
            }
            else
            {
                index = (int)reader.ReadVarUInt32() + 1;
            }

            // Some values have keys, some don't.
            string key = string.Empty;
            bool hasKey = reader.Reader.ReadBitLSB();
            if (hasKey)
            {
                // Some entries use reference a position in the key history for
                // part of the key. If referencing the history, read the position
                // and size from the buffer, then use those to build the string
                // combined with an extra string read (null terminated).
                // Alternatively, just read the string.
                var useHistory = reader.Reader.ReadBitLSB();
                if (useHistory)
                {
                    var pos = (int)reader.Reader.ReadUInt32LSB(5);
                    var size = (int)reader.Reader.ReadUInt32LSB(5);

                    if (pos >= keys.Count)
                    {
                        key += reader.ReadString();
                    }
                    else
                    {
                        var s = keys[pos];
                        if (size > s.Length)
                        {
                            key += s + reader.ReadString();
                        }
                        else
                        {
                            key += s.Substring(0, size) + reader.ReadString();
                        }
                    }
                }
                else
                {
                    key += reader.ReadString();
                }

                if (keys.Count >= 32)
                {
                    keys.RemoveAt(0);
                }
                keys.Add(key.ToString());
            }

            bool hasValue = reader.Reader.ReadBitLSB();
            if (hasValue)
            {
                var isCompressed = false;
                int bitSize;
                if (stringTable.UserDataFixedSize)
                {
                    bitSize = stringTable.UserDataSizeBits;
                }
                else
                {
                    if ((stringTable.Flags & 0x1) != 0)
                    {
                        isCompressed = reader.Reader.ReadBitLSB();
                    }
                    if (stringTable.VarIntBitCounts)
                    {
                        bitSize = (int)reader.ReadUBitVar() * 8;
                    }
                    else
                    {
                        bitSize = (int)reader.Reader.ReadUInt32LSB(17) * 8;
                    }
                }
                var byteLength = (bitSize + 7) / 8;
                value = new byte[byteLength];
                reader.ReadBitsAsBytes(value, bitSize);

                if (isCompressed)
                {
                    try
                    {
                        int uncompressedLength = Snappy.GetUncompressedLength(value);
                        var tmp = new byte[uncompressedLength];
                        Snappy.Decompress(value, tmp);
                        value = tmp;
                    }
                    catch (Exception ex)
                    {
                        throw new NecronomiconException($"Unable to decode Snappy-compressed string table item", ex);
                    }
                }
            }

            if (stringTable.Items.TryGetValue(index, out var updateItem))
            {
                if (key != string.Empty && updateItem.Key != key)
                {
                    updateItem.Key = key;
                }

                if (hasValue)
                {
                    updateItem.Value = value;
                }
            }
            else
            {
                stringTable.Items[index] = new StringTableItem(index, key, value);
            }
            // else if (index == entryCount)
            // {
            //     stringTable.Items[index] = new StringTableItem(index, key, value);
            // }
            // else
            // {
            //     throw new NecronomiconException("StringTable index > entryCount");
            // }
        }
    }
}

public class StringTableItem
{
    public int Index { get; set; }
    public string Key { get; set; } = string.Empty;
    public byte[] Value { get; set; } = null!;
    public StringTableItem(int index, string key, byte[] value)
    {
        Index = index;
        Key = key;
        Value = value;
    }
}

public class StringTable
{
    public int Index { get; set; }
    public string Name { get; set; }
    public Dictionary<int, StringTableItem> Items { get; set; } = new Dictionary<int, StringTableItem>();
    public bool UserDataFixedSize { get; set; }
    public int UserDataSize { get; set; }
    public int UserDataSizeBits { get; set; }
    public int Flags { get; set; }
    public bool VarIntBitCounts { get; set; }

    public StringTable(int index, CSVCMsg_CreateStringTable createStringTable)
    {
        Index = index;
        Name = createStringTable.Name;
        Items = new Dictionary<int, StringTableItem>();
        UserDataFixedSize = createStringTable.UserDataFixedSize;
        UserDataSize = createStringTable.UserDataSize;
        UserDataSizeBits = createStringTable.UserDataSizeBits;
        Flags = createStringTable.Flags;
        VarIntBitCounts = createStringTable.UsingVarintBitcounts;
    }
}