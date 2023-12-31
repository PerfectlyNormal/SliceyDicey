using System.Threading;
using System.Threading.Tasks;
using SliceyDicey.Lib.Gcode;

namespace SliceyDicey.Tests.Gcode;

public class GcodeParserTests
{
    [Fact]
    public async Task Parse_Finds_Png_Thumbnails()
    {
        // Arrange
        var name = "calicat_png_thumbnails.gcode";
        var stream = TestHelper.ReadEmbeddedResource(name);
        
        // Act
        var result = await GcodeParser.Parse(name, stream, CancellationToken.None);
        
        // Assert
        result.Thumbnails.Should()
            .HaveCount(4)
            .And.AllSatisfy(x => x.Format.Should().Be("PNG"))
            .And.Contain(x => x.Dimensions == "16x16" && x.Size == 696)
            .And.Contain(x => x.Dimensions == "313x173" && x.Size == 31316)
            .And.Contain(x => x.Dimensions == "440x240" && x.Size == 47060)
            .And.Contain(x => x.Dimensions == "640x480" && x.Size == 87136);
    }
    
    [Fact]
    public async Task Parse_Finds_Qio_Thumbnails()
    {
        // Arrange
        var name = "calicat_qoi_thumbnails.gcode";
        var stream = TestHelper.ReadEmbeddedResource(name);
        
        // Act
        var result = await GcodeParser.Parse(name, stream, CancellationToken.None);
        
        // Assert
        result.Thumbnails.Should()
            .HaveCount(5)
            .And.Contain(x => x.Format == "QOI" && x.Dimensions == "16x16" && x.Size == 584)
            .And.Contain(x => x.Format == "QOI" && x.Dimensions == "313x173" && x.Size == 36632)
            .And.Contain(x => x.Format == "QOI" && x.Dimensions == "440x240" && x.Size == 57860)
            .And.Contain(x => x.Format == "QOI" && x.Dimensions == "480x240" && x.Size == 64524)
            .And.Contain(x => x.Format == "PNG" && x.Dimensions == "640x480" && x.Size == 87156);
    }
    
    [Fact]
    public async Task Parse_Works_Without_Thumbnails()
    {
        // Arrange
        var name = "calicat_no_thumbnails.gcode";
        var stream = TestHelper.ReadEmbeddedResource(name);
        
        // Act
        var result = await GcodeParser.Parse(name, stream, CancellationToken.None);
        
        // Assert
        result.Thumbnails.Should().BeEmpty();
    }
}