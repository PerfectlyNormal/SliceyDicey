using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.BinaryGcode.Blocks;
using SliceyDicey.Lib.Hash;

namespace SliceyDicey.Lib.BinaryGcode;

public abstract class Block
{
    public Block(BlockHeader header)
    {
        Header = header;
        Checksum = new BlockChecksum(BlockChecksumType.None, null, null, null, null);
    }

    public BlockHeader Header { get; }
    public BlockChecksum Checksum { get; private set; }
    public abstract int ParameterSize { get; }

    public BlockType Type => Header.Type;
    public CompressionType Compression => Header.Compression;
    public uint UncompressedSize => Header.UncompressedSize;
    public uint CompressedSize => Header.CompressedSize;

    public abstract void ReadParameters(Span<byte> data, int blockSize);
    public abstract void Read(Span<byte> data, int blockSize, ILogger logger);

    public void ReadChecksum(BlockChecksumType checksumType, Span<byte> headerBlock, Span<byte> payload,
        Span<byte> checksum, int blockSize,
        ILogger? logger)
    {
        Checksum = new BlockChecksum(checksumType, headerBlock, payload, checksum[..blockSize], logger);
    }

    public static Block ReadBlock(Memory<byte> blockData)
    {
        var header = new BlockHeader(blockData[..12].Span);

        return header.Type switch
        {
            BlockType.FileMetadata => new FileMetadataBlock(header),
            BlockType.PrinterMetadata => new PrinterMetadataBlock(header),
            BlockType.Thumbnail => new ThumbnailBlock(header),
            BlockType.PrintMetadata => new PrintMetadataBlock(header),
            BlockType.SlicerMetadata => new SlicerMetadataBlock(header),
            BlockType.Gcode => new GcodeBlock(header),
            _ => throw new ArgumentOutOfRangeException(nameof(blockData), header.Type, "Unexpected block type"),
        };
    }
}

public class BlockChecksum
{
    public BlockChecksum(BlockChecksumType checksumType, Span<byte> header, Span<byte> payload, Span<byte> expected,
        ILogger? logger)
    {
        ChecksumType = checksumType;

        switch (ChecksumType)
        {
            case BlockChecksumType.None:
                Valid = true;
                break;
            case BlockChecksumType.Crc32:
            {
                var crc32 = Crc32.Hash(header[..2].ToArray(), 0, logger);
                crc32 = Crc32.Hash(header[2..4].ToArray(), crc32, logger);
                crc32 = Crc32.Hash(header[4..8].ToArray(), crc32, logger);
                crc32 = Crc32.Hash(header[8..].ToArray(), crc32, logger);
                crc32 = Crc32.Hash(payload.ToArray(), crc32, logger);
                
                var raw = BitConverter.GetBytes(crc32).AsSpan();
                
                Valid = raw.SequenceEqual(expected);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(checksumType), checksumType.ToString(),
                    "Unexpected checksum type");
        }
    }

    public BlockChecksumType ChecksumType { get; }
    public bool Valid { get; }
}
