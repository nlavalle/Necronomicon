using System.Buffers;
using necronomicon.processor;
using Snappier;
using Steam.Protos.Dota2;

namespace necronomicon.model.engine;

public class StringTables
{
    public List<StringTable> Tables = new List<StringTable>();
    public Dictionary<string, int> NameIndex = new Dictionary<string, int>();
}

public class SvcStringTable
{
    private readonly CircularBuffer<string> _circularBuffer = new CircularBuffer<string>(5);
    private readonly char[] _charBuffer = new char[2048];

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
        StringTable newStringTable = new StringTable(createStringTable);

        byte[]? rented = null;
        ReadOnlySpan<byte> messageBuffer;
        if (createStringTable.DataCompressed)
        {
            var compressedBuffer = createStringTable.StringData.Span;
            int uncompressedLength = Snappy.GetUncompressedLength(compressedBuffer);
            rented = ArrayPool<byte>.Shared.Rent(uncompressedLength);

            var span = rented.AsSpan(0, uncompressedLength);
            Snappy.Decompress(compressedBuffer, span);

            messageBuffer = span;
        }
        else
        {
            messageBuffer = createStringTable.StringData.Span;
        }

        // Parse the items out of the string table data
        ParseStringTable(messageBuffer, createStringTable.NumEntries, newStringTable);

        if (rented is not null)
            ArrayPool<byte>.Shared.Return(rented);

        // // Add the table to the parser state
        _parser.StringTables.Tables.Add(newStringTable);
        _parser.StringTables.NameIndex[newStringTable.Name] = stringTableIndex;

        // Apply the updates to baseline state
        if (newStringTable.Name == "instancebaseline")
        {
            _parser.UpdateInstanceBaseline();
        }

        await Task.CompletedTask;
    }

    public async Task OnCSVCMsgUpdateStringTable(CSVCMsg_UpdateStringTable updateStringTable)
    {
        var tableId = updateStringTable.TableId;
        if (tableId < _parser.StringTables.Tables.Count)
        {
            var stringTable = _parser.StringTables.Tables[tableId];

            ParseStringTable(updateStringTable.StringData.Span, updateStringTable.NumChangedEntries, stringTable);

            // Apply the updates to baseline state
            if (stringTable.Name == "instancebaseline")
            {
                _parser.UpdateInstanceBaseline();
            }
        }
        await Task.CompletedTask;
    }


    public void ParseStringTable(ReadOnlySpan<byte> stringTableBuffer, int numEntries, StringTable stringTable)
    {
        if (stringTableBuffer.Length == 0)
        {
            return;
        }

        var reader = new FastBitReader(stringTableBuffer);
        var chars = _charBuffer.AsSpan();

        // Maintain a list of key history
        var keys = _circularBuffer;

        byte[]? compressedData = null;

        // Start with an index of -1.
        // If the first item is at index 0 it will use a incr operation.
        var index = -1;

        for (int i = 0; i < numEntries; i++)
        {
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

            var updateItem = stringTable.Items[index];
            // Some values have keys, some don't.
            bool hasKey = reader.Reader.ReadBitLSB();
            if (hasKey)
            {
                string key = string.Empty;
                int keyIndex = 0;

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

                    if (pos < keys.Count)
                    {
                        var s = keys[pos];

                        // I'm worried about this, the number of UTF-16 (string)
                        // characters is not equivalent to the number of UTF-8
                        // characters (bytes) indicated by "size" here.
                        size = Math.Min(size, s.Length);

                        s.AsSpan(0, size).CopyTo(chars.Slice(keyIndex));
                        keyIndex += size;
                    }
                }

                keyIndex += reader.ReadString(chars.Slice(keyIndex));

                if (keyIndex > 0)
                {
                    key = string.Create(keyIndex, _charBuffer, static (span, buffer) =>
                    {
                        buffer.AsSpan(0, span.Length).CopyTo(span);
                    });

                    updateItem.Key = key;
                }

                keys.Add(key.ToString());
            }

            bool hasValue = reader.Reader.ReadBitLSB();
            if (hasValue)
            {
                byte[] value;
                int bitSize;
                int byteLength;

                var isCompressed = (stringTable.Flags & 0x1) != 0 && reader.Reader.ReadBitLSB();

                if (stringTable.UserDataFixedSize)
                {
                    bitSize = stringTable.UserDataSizeBits;
                    byteLength = (bitSize + 7) >> 3;
                }
                else
                {
                    // TODO: I think the second one should be multiplied by 8, and the other one should be rounded to 8.
                    /*
                        if (stringTable.VarIntBitCounts)
                        {
                            bitSize = (int)reader.ReadUBitVar();
                            byeLength = (bitSize * 7) >> 3;
                        }
                        else
                        {
                            byteLength = (int)reader.Reader.ReadUInt32LSB(17);
                            bitSize = byteLength * 8;
                        }
                    */
                    if (stringTable.VarIntBitCounts)
                    {
                        byteLength = (int)reader.ReadUBitVar();
                    }
                    else
                    {
                        byteLength = (int)reader.Reader.ReadUInt32LSB(17);
                    }

                    bitSize = byteLength * 8;
                }

                if (isCompressed)
                {
                    var span = ProcessHelpers.ResolveArray(ref compressedData, byteLength);
                    reader.ReadBitsAsBytes(span, bitSize);

                    try
                    {
                        int uncompressedLength = Snappy.GetUncompressedLength(span);
                        value = new byte[uncompressedLength];
                        Snappy.Decompress(span.ToArray(), value);
                    }
                    catch (Exception ex)
                    {
                        throw new NecronomiconException($"Unable to decode Snappy-compressed string table item", ex);
                    }
                    finally
                    {
                        if (compressedData is not null)
                            ArrayPool<byte>.Shared.Return(compressedData);
                    }
                }
                else
                {
                    value = new byte[byteLength];
                    reader.ReadBitsAsBytes(value, bitSize);
                }

                updateItem.Value = value;
            }
        }

        if (compressedData is not null)
            ArrayPool<byte>.Shared.Return(compressedData);

        keys.Reset();
    }

    private class CircularBuffer<T>
    {
        private readonly T[] _data;
        private readonly int _mask;
        private int _position;

        public int Count => _position < _data.Length ? _position : _data.Length;

        public CircularBuffer(int bitCount)
        {
            var shifted = 1 << bitCount;
            _data = new T[shifted];
            _position = 0;
            _mask = shifted - 1;
        }

        public ref T this[int index]
        {
            get
            {
                int position = _position;
                if (position >= _data.Length)
                {
                    index = (index + position) & _mask;
                }
                else
                {
                    if (index >= position)
                        throw new ArgumentOutOfRangeException(nameof(index));
                }

                return ref _data[index];
            }
        }

        public void Add(T value)
        {
            var position = _position;
            _data[position & _mask] = value;
            _position = position + 1;
        }

        public void Reset(bool clear = false)
        {
            if (clear)
                _data.AsSpan().Clear();

            _position = 0;
        }
    }
}

public class StringTableItem
{
    public string Key { get; set; }
    public byte[] Value { get; set; }

    public StringTableItem()
    {
        Key = string.Empty;
        Value = [];
    }

    public StringTableItem(string key, byte[] value)
    {
        Key = key;
        Value = value;
    }
}

public class StringTable
{
    public string Name { get; set; }
    public GappedList<StringTableItem> Items { get; set; }
    public bool UserDataFixedSize { get; set; }
    public int UserDataSize { get; set; }
    public int UserDataSizeBits { get; set; }
    public int Flags { get; set; }
    public bool VarIntBitCounts { get; set; }

    public StringTable(CSVCMsg_CreateStringTable createStringTable)
    {
        Name = createStringTable.Name;
        Items = new GappedList<StringTableItem>();
        UserDataFixedSize = createStringTable.UserDataFixedSize;
        UserDataSize = createStringTable.UserDataSize;
        UserDataSizeBits = createStringTable.UserDataSizeBits;
        Flags = createStringTable.Flags;
        VarIntBitCounts = createStringTable.UsingVarintBitcounts;
    }
}
