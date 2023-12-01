using SliceyDicey.Lib.BinaryGcode.Blocks;

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

    public Block AddBlock(Memory<byte> blockData)
    {
        var block = Block.ReadBlock(blockData);
        _blocks.Add(block);
        return block;
    }
}
