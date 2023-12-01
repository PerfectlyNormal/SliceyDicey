using System.Buffers;
using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.Extensions;

namespace SliceyDicey.Lib.BinaryGcode;

public class Parser
{
    private readonly ILogger _logger;

    public Parser(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<BinaryGcodeFile> Parse(string name, Stream input, CancellationToken cancellationToken)
    {
        var file = new BinaryGcodeFile
        {
            Name = name
        };

        // Read file header
        using var headerMemoryOwner = MemoryPool<byte>.Shared.Rent(Header.Size);
        var headerMemory = headerMemoryOwner.Memory;
        var readBytes = await input.ReadAsync(headerMemory, cancellationToken);
        switch (readBytes)
        {
            case < Header.Size:
                throw new ArgumentException("Unexpected length of file. Unable to read header.");
            case > Header.Size:
            {
                // We read a bit much, seek back
                var offset = Header.Size - readBytes;
                var position = input.Position;
                input.Seek(offset, SeekOrigin.Current);
                _logger.LogDebug(
                    "Read a bit much (read {ReadBytes}, expected {ExpectedBytes}). Seeking {Offset} backwards from position {Position} to {NewPosition}",
                    readBytes, Header.Size, offset, position, input.Position);
                break;
            }
        }

        file.Header = new Header(headerMemory.Span);
        _logger.LogTrace("Finished reading header. Position is {Position}", input.Position);

        // Read blocks
        while (input.CanRead && input.Position < input.Length)
        {
            using var blockHeaderMemory = await ReadData(input, BlockHeader.HeaderSize, cancellationToken);
            var lastBlock = file.AddBlock(blockHeaderMemory.Data);
            _logger.LogDebug("Read block header ({Type}). Position is {Position}", lastBlock.Type, input.Position);

            if (lastBlock.Header.Compression == CompressionType.None)
            {
                // No compression, so no compression size in header. Rewind slightly.
                var position = input.Position;
                const int offset = BlockHeader.UncompressedHeaderSize - BlockHeader.HeaderSize;
                input.Seek(offset, SeekOrigin.Current);
                _logger.LogTrace(
                    "Uncompressed block. Seeking {Offset} bytes backwards from position {Position} to {NewPosition}",
                    offset, position,
                    input.Position);
            }

            // Read block parameters
            using var blockParamMemory = await ReadData(input, lastBlock.ParameterSize, cancellationToken);
            lastBlock.ReadParameters(blockParamMemory.Span, lastBlock.ParameterSize);

            // Read block data
            var blockSize = lastBlock.Header.HasCompressedData
                ? lastBlock.Header.CompressedSize
                : lastBlock.Header.UncompressedSize;
            using var blockMemory = await ReadData(input, (int)blockSize, cancellationToken);
            lastBlock.Read(blockMemory.Span, (int)blockSize, _logger);

            // Read and verify checksum
            if (file.Header.ChecksumType == BlockChecksumType.Crc32)
            {
                const int checksumBlockSize = 4;
                var blockHeaderSize = lastBlock.Compression == CompressionType.None
                    ? BlockHeader.UncompressedHeaderSize
                    : BlockHeader.HeaderSize;
                
                var checksumMemory = await ReadData(input, checksumBlockSize, cancellationToken);
                lastBlock.ReadChecksum(file.Header.ChecksumType,
                    blockHeaderMemory.Span[..blockHeaderSize], blockParamMemory.Span.Concat(blockMemory.Span),
                    checksumMemory.Span, checksumBlockSize, _logger);
            }
        }

        return file;
    }

    private async Task<MemorySpace> ReadData(Stream input, int blockSize, CancellationToken cancellationToken)
    {
        var owner = MemoryPool<byte>.Shared.Rent(blockSize);
        var memory = owner.Memory;
        var readBytes = await input.ReadAsync(memory, cancellationToken);
        if (readBytes <= blockSize) return new MemorySpace(owner, memory, readBytes);

        // We read a bit much, seek back
        var offset = blockSize - readBytes;
        var position = input.Position;
        input.Seek(offset, SeekOrigin.Current);
        _logger.LogTrace(
            "Read a bit much (read {ReadBytes}, expected {ExpectedBytes}). Seeking {Offset} backwards from position {Position} to {NewPosition}",
            readBytes, blockSize, offset, position, input.Position);

        return new MemorySpace(owner, memory, blockSize);
    }
}

internal class MemorySpace : IDisposable
{
    private readonly IMemoryOwner<byte> _owner;
    private readonly int _blockSize;

    public MemorySpace(IMemoryOwner<byte> owner, Memory<byte> memory, int blockSize)
    {
        _owner = owner;
        _blockSize = blockSize;
        Data = memory;
    }

    public Memory<byte> Data { get; }
    public Span<byte> Span => Data.Span[.._blockSize];

    public void Dispose()
    {
        _owner.Dispose();
    }
}
