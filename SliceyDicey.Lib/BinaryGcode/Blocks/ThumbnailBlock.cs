using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.Gcode;

namespace SliceyDicey.Lib.BinaryGcode.Blocks;

public class ThumbnailBlock : Block
{
    public ThumbnailBlock(BlockHeader header) : base(header)
    {
    }

    public override int ParameterSize => 6;
    public ThumbnailFormat Format { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public IGcodeThumbnail Thumbnail { get; set; }
    public string Dimensions => $"{Width}x{Height}";

    public override void Read(Span<byte> data, int blockSize, ILogger logger)
    {
        if (blockSize >= 0 && blockSize <= data.Length)
            Thumbnail = Format switch
            {
                ThumbnailFormat.Png => new GcodePngThumbnail(Dimensions, blockSize, data[..blockSize]),
                ThumbnailFormat.Qoi => new GcodeQoiThumbnail(Dimensions, blockSize, data[..blockSize]),
                _ => throw new InvalidOperationException($"Unsupported thumbnail format {Format.ToString()} encountered"),
            };
        else
            throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize,
                $"Not enough data passed in. Expected to find at least {blockSize} bytes, but found {data.Length}");
    }

    public override void ReadParameters(Span<byte> data, int blockSize)
    {
        Format = (ThumbnailFormat)BitConverter.ToUInt16(data[..2]);
        Width = BitConverter.ToUInt16(data[2..4]);
        Height = BitConverter.ToUInt16(data[4..6]);
    }
}

public enum ThumbnailFormat
{
    Png = 0,
    Jpeg = 1,
    Qoi = 2,
}
