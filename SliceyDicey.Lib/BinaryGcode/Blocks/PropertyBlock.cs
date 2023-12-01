using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.Extensions;

namespace SliceyDicey.Lib.BinaryGcode.Blocks;

public abstract class PropertyBlock : Block
{
    public override int ParameterSize => 2;

    protected PropertyBlock(BlockHeader header) : base(header)
    {
    }

    public override void ReadParameters(Span<byte> data, int blockSize)
    {
        Encoding = (MetadataEncoding)BitConverter.ToUInt16(data[..2]);
    }

    public override void Read(Span<byte> data, int blockSize, ILogger logger)
    {
        if (Encoding == MetadataEncoding.Ini)
            Properties = data.ReadAsIni(blockSize, Compression, logger);
    }

    public bool TryGetValue(string propertyName, out string? result)
        => Properties.TryGetValue(propertyName, out result);

    public MetadataEncoding Encoding { get; set; }
    public Dictionary<string, string> Properties { get; private set; } = new();
}
