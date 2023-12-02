using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoSmart.StreamCompare;
using SliceyDicey.Lib.Gcode;

namespace SliceyDicey.Tests.Gcode;

public class GcodeQoiThumbnailTests
{
    private readonly StreamCompare _comparer = new();

    [Fact]
    public async Task Can_Create_Png()
    {
        // Arrange
        var name = "calicat_qoi_thumbnails.gcode";
        var stream = TestHelper.ReadEmbeddedResource(name);
        var result = await GcodeParser.Parse(name, stream, CancellationToken.None);
        var thumbnail = result.Thumbnails.First(x => x is { Format: "QOI", Dimensions: "16x16" });

        var expectedResult = TestHelper.ReadEmbeddedResource("calicat_16x16_from_qoi.png", "output");
        
        // Act
        var png = thumbnail.GeneratePng();
        
        // Assert
        png.Length.Should().Be(expectedResult.Length);
    }
    
    [Fact]
    public async Task Rewinds_The_Stream()
    {
        // Arrange
        var name = "calicat_qoi_thumbnails.gcode";
        var stream = TestHelper.ReadEmbeddedResource(name);
        var result = await GcodeParser.Parse(name, stream, CancellationToken.None);
        var thumbnail = result.Thumbnails.First(x => x is { Format: "QOI", Dimensions: "16x16" });

        var expectedResult = TestHelper.ReadEmbeddedResource("calicat_16x16_from_qoi.png", "output");
        
        // Act
        var png = thumbnail.GeneratePng();
        
        // Assert
        png.Position.Should().Be(0);
    }
}