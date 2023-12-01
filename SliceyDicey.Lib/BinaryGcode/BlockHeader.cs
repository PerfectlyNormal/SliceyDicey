namespace SliceyDicey.Lib.BinaryGcode;

public class BlockHeader
{
    public const int UncompressedHeaderSize = 8;
    public const int HeaderSize = 12;
    
    public BlockHeader(Span<byte> data)
    {
        Type = (BlockType)BitConverter.ToUInt16(data[..2]);
        Compression = (CompressionType)BitConverter.ToUInt16(data[2..4]);
        UncompressedSize = BitConverter.ToUInt32(data[4..8]);
        
        if (Compression != CompressionType.None)
            CompressedSize = BitConverter.ToUInt32(data[8..12]);
    }

    public BlockType Type { get; set; }
    public CompressionType Compression { get; set; }
    public uint UncompressedSize { get; set; }
    public uint CompressedSize { get; set; }
    public bool HasCompressedData => CompressedSize > 0;
}
