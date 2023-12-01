using SliceyDicey.Lib.BinaryGcode.Blocks;
using SliceyDicey.Lib.Gcode;

namespace SliceyDicey.Lib.BinaryGcode;

public class BinaryGcodeFile
{
    private readonly List<Block> _blocks = new();

    public string Name { get; set; }
    public Header Header { get; set; }
    public IEnumerable<Block> Blocks => _blocks.AsReadOnly();

    public FileMetadataBlock? FileMetadata
        => (FileMetadataBlock?)Blocks.SingleOrDefault(x => x.Type == BlockType.FileMetadata);

    public PrinterMetadataBlock? PrinterMetadata
        => (PrinterMetadataBlock?)Blocks.SingleOrDefault(x => x.Type == BlockType.PrinterMetadata);

    public IEnumerable<ThumbnailBlock> Thumbnails
        => Blocks
            .Where(x => x.Type == BlockType.Thumbnail)
            .Cast<ThumbnailBlock>();

    public PrintMetadataBlock? PrintMetadata
        => (PrintMetadataBlock?)Blocks.SingleOrDefault(x => x.Type == BlockType.PrintMetadata);

    public SlicerMetadataBlock? SlicerMetadata
        => (SlicerMetadataBlock?)Blocks.SingleOrDefault(x => x.Type == BlockType.SlicerMetadata);

    public IEnumerable<GcodeBlock> GcodeBlocks
        => Blocks
            .Where(x => x.Type == BlockType.Gcode)
            .Cast<GcodeBlock>();

    public IEnumerable<GcodeInstruction> Instructions
        => GcodeBlocks
            .SelectMany(x => x.Instructions);

    public IEnumerable<GcodeInstruction> Comments
        => Instructions.Where(x => x.OnlyComment);

    public IEnumerable<GcodeInstruction> Commands
        => Instructions.Where(x => x.HasCommand);

    public Block AddBlock(Memory<byte> blockData)
    {
        var block = Block.ReadBlock(blockData);
        _blocks.Add(block);
        return block;
    }
}
