using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.BinaryGcode;
using SliceyDicey.Lib.BinaryGcode.Blocks;
using Xunit.Abstractions;

namespace SliceyDicey.Tests.BinaryGcode;

public class ParserTests
{
    public ParserTests(ITestOutputHelper output)
    {
        Sut = new Parser(output.BuildLoggerFor<Parser>(LogLevel.Debug));
    }

    public Parser Sut { get; }

    [Fact]
    public async Task Can_Parse_Header()
    {
        // Arrange
        var name = "calicat_qoi_thumbnails_binary.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);

        // Assert
        result.Header.Should().NotBeNull();
        result.Header.MagicNumber.Should().Be("GCDE");
        result.Header.Version.Should().Be(1);
        result.Header.ChecksumType.Should().Be(BlockChecksumType.Crc32);
    }

    [Fact]
    public async Task Can_Parse_Block_Header()
    {
        // Arrange
        var name = "calicat_qoi_thumbnails_binary.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);

        // Assert
        result.Blocks.Should().NotBeEmpty();
        var block = result.Blocks.First();
        block.Type.Should().Be(BlockType.FileMetadata);
        block.Compression.Should().Be(CompressionType.None);
        block.UncompressedSize.Should().Be(39u);
        block.CompressedSize.Should().Be(0);
    }

    [Fact]
    public async Task Can_Read_All_Blocks()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);

        // Assert
        result.Blocks.Should().NotBeEmpty()
            .And.HaveCount(16)
            .And.ContainSingle(x => x.Type == BlockType.FileMetadata)
            .And.ContainSingle(x => x.Type == BlockType.PrinterMetadata)
            .And.ContainSingle(x => x.Type == BlockType.SlicerMetadata)
            .And.ContainSingle(x => x.Type == BlockType.PrintMetadata);
        result.Blocks.Where(x => x.Type == BlockType.Thumbnail).Should().HaveCount(2);
        result.Blocks.Where(x => x.Type == BlockType.Gcode).Should().HaveCount(10);
    }

    // FIXME: Add test for Heatshrink11
    
    [Fact]
    public async Task Can_Unpack_Heatshrink12()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);

        // Assert
        result.GcodeBlocks.Should().NotBeEmpty()
            .And.Contain(x => x.Compression == CompressionType.Heatshrink12);
        
        var shrunkBlock = result.GcodeBlocks.Last(x => x.Compression == CompressionType.Heatshrink12);
        shrunkBlock.Instructions.Should()
            .NotBeEmpty()
            .And.HaveCount(1487);

        shrunkBlock.Instructions.ElementAt(90).Command.Should().Be("G1 X85.078 Y82.437 E.04763");
        shrunkBlock.Instructions.ElementAt(91).Command.Should().Be("G1 X85.323 Y82.827 E.012");
        shrunkBlock.Instructions.ElementAt(129).Comment.Should().Be("stop printing object Shape-Box id:0 copy 0");
        shrunkBlock.Instructions.ElementAt(1485).Command.Should().Be("M73 P100 R0");
        shrunkBlock.Instructions.ElementAt(1486).Command.Should().Be("");
    }

    [Fact]
    public async Task FileMetadata_Parses_Values()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var fileMetadata = result.FileMetadata;

        // Assert
        fileMetadata.Should().NotBeNull();
        fileMetadata!.Compression.Should().Be(CompressionType.None);
        fileMetadata.Properties.Should()
            .NotBeEmpty()
            .And.Contain(KeyValuePair.Create("Producer", "PrusaSlicer 2.6.0"));
    }

    [Fact]
    public async Task PrinterMetadata_Parses_Values()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var printerMetadata = result.PrinterMetadata;

        // Assert
        printerMetadata.Should().NotBeNull();
        printerMetadata!.Compression.Should().Be(CompressionType.None);
        printerMetadata.Properties.Should()
            .NotBeEmpty()
            .And.Contain(KeyValuePair.Create("printer_model", "MINI"))
            .And.Contain(KeyValuePair.Create("filament_type", "PETG"))
            .And.Contain(KeyValuePair.Create("nozzle_diameter", "0.4"))
            .And.Contain(KeyValuePair.Create("bed_temperature", "90"))
            .And.Contain(KeyValuePair.Create("brim_width", "0"))
            .And.Contain(KeyValuePair.Create("fill_density", "15%"))
            .And.Contain(KeyValuePair.Create("layer_height", "0.15"))
            .And.Contain(KeyValuePair.Create("temperature", "240"))
            .And.Contain(KeyValuePair.Create("ironing", "0"))
            .And.Contain(KeyValuePair.Create("support_material", "0"))
            .And.Contain(KeyValuePair.Create("max_layer_z", "18.05"))
            .And.Contain(KeyValuePair.Create("extruder_colour", "\"\""))
            .And.Contain(KeyValuePair.Create("filament used [mm]", "986.61"))
            .And.Contain(KeyValuePair.Create("filament used [cm3]", "2.37"))
            .And.Contain(KeyValuePair.Create("filament used [g]", "3.01"))
            .And.Contain(KeyValuePair.Create("filament cost", "0.08"))
            .And.Contain(KeyValuePair.Create("estimated printing time (normal mode)", "32m 6s"))
            .And.HaveCount(17);
    }

    [Fact]
    public async Task Validates_Block_Checksums()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var printerMetadata = result.PrinterMetadata;

        // Assert
        printerMetadata.Should().NotBeNull();
        printerMetadata!.Checksum.Should().NotBeNull();
        printerMetadata.Checksum.ChecksumType.Should().Be(BlockChecksumType.Crc32);
        printerMetadata.Checksum.Valid.Should().BeTrue();

        result.Blocks.Should().AllSatisfy(x => x.Checksum.Valid.Should().BeTrue());
    }

    [Fact]
    public async Task ThumbnailMetadata_Parses_Values()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var thumbnails = result.Thumbnails;

        // Assert
        thumbnails.Should().HaveCount(2).And.SatisfyRespectively(new[]
        {
            (ThumbnailBlock block) =>
            {
                block.Format.Should().Be(ThumbnailFormat.Png);
                block.Thumbnail.Should().NotBeNull();
                block.Thumbnail.Size.Should().Be(block.UncompressedSize);
                block.Width.Should().Be(16);
                block.Height.Should().Be(16);
            },
            (ThumbnailBlock block) =>
            {
                block.Format.Should().Be(ThumbnailFormat.Png);
                block.Thumbnail.Should().NotBeNull();
                block.Thumbnail.Size.Should().Be(block.UncompressedSize);
                block.Width.Should().Be(220);
                block.Height.Should().Be(124);
            },
        });
    }

    [Fact]
    public async Task PrintMetadata_Parses_Values()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var printMetadata = result.PrintMetadata;

        // Assert
        printMetadata.Should().NotBeNull();
        printMetadata!.Compression.Should().Be(CompressionType.None);
        printMetadata!.Properties.Should()
            .NotBeEmpty()
            .And.Contain(KeyValuePair.Create("filament used [mm]", "986.61"))
            .And.Contain(KeyValuePair.Create("filament used [cm3]", "2.37"))
            .And.Contain(KeyValuePair.Create("filament used [g]", "3.01"))
            .And.Contain(KeyValuePair.Create("filament cost", "0.08"))
            .And.Contain(KeyValuePair.Create("total filament used [g]", "3.01"))
            .And.Contain(KeyValuePair.Create("total filament cost", "0.08"))
            .And.Contain(KeyValuePair.Create("estimated printing time (normal mode)", "32m 6s"))
            .And.Contain(KeyValuePair.Create("estimated first layer printing time (normal mode)", "1m 8s"))
            .And.HaveCount(8);
    }

    [Fact]
    public async Task SlicerMetadata_Parses_Values()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var metadata = result.SlicerMetadata;

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Compression.Should().Be(CompressionType.Deflate);
        metadata.Properties.Should()
            .NotBeEmpty()
            .And.HaveCount(302);
    }

    [Fact]
    public async Task Gcode_Parses_MeatPack_With_Comments()
    {
        // Arrange
        var name = "mini_cube_b.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var gcodeBlocks = result.GcodeBlocks.ToList();

        // Assert
        gcodeBlocks.Should()
            .NotBeNull()
            .And.HaveCount(10);

        var block = gcodeBlocks.First();
        block.Compression.Should().Be(CompressionType.Heatshrink12);
        block.CompressedSize.Should().Be(14537);
        block.UncompressedSize.Should().Be(37969);
        block.Encoding.Should().Be(GcodeEncoding.MeatPackComments);
        block.Instructions.Should().HaveCount(2779);
        var commands = block.Instructions.Where(x => x.HasCommand).ToList();

        block.Instructions.First().Comment.Should().Be("external perimeters extrusion width = 0.45mm");
        commands.ElementAt(0).Command.Should().Be("M73 P0 R32");
        commands.ElementAt(1).Command.Should().Be("M201 X2500 Y2500 Z400 E5000");
        commands.ElementAt(7).Command.Should().Be("G90");
    }
    
    [Fact]
    public async Task Gcode_Parses_MeatPack_Without_Comments()
    {
        // Arrange
        var name = "calicat_meatpack_no_comments.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var gcodeBlocks = result.GcodeBlocks.ToList();

        // Assert
        gcodeBlocks.Should()
            .NotBeNull()
            .And.HaveCount(10);

        var block = gcodeBlocks.First();
        block.Compression.Should().Be(CompressionType.None);
        block.CompressedSize.Should().Be(0);
        block.UncompressedSize.Should().Be(31059);
        block.Encoding.Should().Be(GcodeEncoding.MeatPack);
        block.Instructions.Should().HaveCount(2429);
        
        block.Instructions.Where(x => x.OnlyComment).Should().BeEmpty();
        
        var commands = block.Instructions.Where(x => x.HasCommand).ToList();
        commands.ElementAt(0).Command.Should().Be("M73 P0 R21");
        commands.ElementAt(1).Command.Should().Be("M201 X4000 Y4000 Z200 E2500");
    }
    
    [Fact]
    public async Task Gcode_Parses_Unpacked_Instructions()
    {
        // Arrange
        var name = "filament_cap_hex_1h9m_0.10mm_205C_PLA_CR10SMARTPRO.bgcode";
        var stream = TestHelper.ReadEmbeddedResource(name);

        // Act
        var result = await Sut.Parse(name, stream, CancellationToken.None);
        var gcodeBlocks = result.GcodeBlocks.ToList();

        // Assert
        gcodeBlocks.Should()
            .NotBeNull()
            .And.HaveCount(71);

        var block = gcodeBlocks.First();
        block.Compression.Should().Be(CompressionType.None);
        block.CompressedSize.Should().Be(0);
        block.UncompressedSize.Should().Be(65522);
        block.Encoding.Should().Be(GcodeEncoding.None);
        block.Instructions.Should().HaveCount(2697);
        block.Instructions.Where(x => x.OnlyComment).Should().HaveCount(684);
        block.Instructions.Where(x => x is { HasCommand: true, HasComment: false }).Should().HaveCount(1993);
        block.Instructions.Where(x => x is { HasCommand: true, HasComment: true }).Should().HaveCount(19);
        block.Instructions.Where(x => x is { HasCommand: false, HasComment: false }).Should().ContainSingle();
    }
}
