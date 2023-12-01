using System.Text;

namespace SliceyDicey.Lib.BinaryGcode;

public class Header
{
    public const int Size = 10;
    
    public Header(Span<byte> headerBytes)
    {
        MagicNumber = Encoding.ASCII.GetString(headerBytes[..4]);
        Version = BitConverter.ToUInt32(headerBytes[4..8]);
        ChecksumType = (BlockChecksumType)BitConverter.ToUInt16(headerBytes[8..10]);
    }

    public string MagicNumber { get; set; }
    public uint Version { get; set; }
    public BlockChecksumType ChecksumType { get; set; }
}

public enum BlockChecksumType
{
    None = 0,
    Crc32 = 1,
}
