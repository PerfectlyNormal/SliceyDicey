using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.Extensions;
using SliceyDicey.Lib.Gcode;
using SliceyDicey.Lib.Meatpack;

namespace SliceyDicey.Lib.BinaryGcode.Blocks;

public class GcodeBlock : Block
{
    private readonly List<GcodeInstruction> _instructions;

    public GcodeBlock(BlockHeader header) : base(header)
    {
        _instructions = new List<GcodeInstruction>();
    }

    public override int ParameterSize => 2;

    public GcodeEncoding Encoding { get; set; }
    public IEnumerable<GcodeInstruction> Instructions => _instructions.AsReadOnly();

    public override void ReadParameters(Span<byte> data, int blockSize)
    {
        Encoding = (GcodeEncoding)BitConverter.ToUInt16(data[..2]);
    }

    public override void Read(Span<byte> data, int blockSize, ILogger logger)
    {
        switch (Encoding)
        {
            case GcodeEncoding.None:
                var unpacked = data.ReadAsText(blockSize, System.Text.Encoding.ASCII, Compression, logger);
                _instructions.AddRange(unpacked
                    .Split("\n")
                    .Select((line, idx) => new GcodeInstruction(idx, line)));
                break;
            case GcodeEncoding.MeatPack:
            case GcodeEncoding.MeatPackComments:
                var decoder = new MeatpackDecoder();
                var unpackedBytes = data.ReadAsBytes(blockSize, Compression, logger);
                var text = decoder.Unbinarize(unpackedBytes);
                _instructions.AddRange(text
                    .Split("\n")
                    .Select((line, idx) => new GcodeInstruction(idx, line)));
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unsupported Gcode encoding {Encoding.ToString()} encountered.");
        }
    }
}

public enum GcodeEncoding
{
    None = 0,

    /// <summary>
    /// MeatPack algorithm
    /// </summary>
    MeatPack = 1,

    /// <summary>
    ///  MeatPack algorithm modified to keep comment lines
    /// </summary>
    MeatPackComments = 2,
}
