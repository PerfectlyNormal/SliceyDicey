using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoSmart.StreamCompare;
using SliceyDicey.Lib.Gcode;

namespace SliceyDicey.Tests.Gcode;

public class GcodePngThumbnailTests
{
    private readonly StreamCompare _comparer = new();

    [Fact]
    public async Task Can_Create_Png()
    {
        // Arrange
        var name = "calicat_png_thumbnails.gcode";
        var stream = TestHelper.ReadEmbeddedResource(name);
        var result = await GcodeParser.Parse(name, stream, CancellationToken.None);
        var thumbnail = result.Thumbnails.First(x => x is { Format: "PNG", Dimensions: "16x16" });

        var expectedResult = TestHelper.ReadEmbeddedResource("calicat_16x16_from_png.png", "output");
        
        // Act
        var png = thumbnail.GeneratePng();
        
        // Assert
        var equal = await _comparer.AreEqualAsync(png, expectedResult);
        equal.Should().BeTrue();
    }
}