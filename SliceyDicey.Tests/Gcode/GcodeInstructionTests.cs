using SliceyDicey.Lib.Gcode;

namespace SliceyDicey.Tests.Gcode;

public class GcodeInstructionTests
{
    [Theory]
    [InlineData(";")]
    [InlineData("; comment")]
    [InlineData(";;;")]
    [InlineData(";WIDTH:0.449999")]
    public void Knows_Its_A_Comment(string input)
    {
        var instruction = new GcodeInstruction(1, input);

        instruction.HasCommand.Should().BeFalse();
        instruction.HasComment.Should().BeTrue();
        instruction.OnlyComment.Should().BeTrue();
    }

    [Theory]
    [InlineData("G1 X100")]
    [InlineData("G1 ; something")]
    [InlineData("G1 X135.452 Y87.199 E.01116 ;WIDTH:0.449999")]
    [InlineData("M204 P1000")]
    public void Knows_Its_A_Command(string input)
    {
        var instruction = new GcodeInstruction(1, input);

        instruction.HasCommand.Should().BeTrue();
        instruction.OnlyComment.Should().BeFalse();
    }

    [Theory]
    [InlineData("G1 ; something")]
    [InlineData("G1 X135.452 Y87.199 E.01116 ;WIDTH:0.449999")]
    public void Handles_Inline_Comments(string input)
    {
        var instruction = new GcodeInstruction(1, input);

        instruction.HasCommand.Should().BeTrue();
        instruction.HasComment.Should().BeTrue();
        instruction.OnlyComment.Should().BeFalse();
    }

    [Theory]
    [InlineData("; comment", "comment")]
    [InlineData(";WIDTH:0.449999", "WIDTH:0.449999")]
    [InlineData("G1 ; something", "something")]
    [InlineData("G1 X135.452 Y87.199 E.01116 ;WIDTH:0.449999", "WIDTH:0.449999")]
    public void Parses_Comment_Text_Correctly(string input, string expected)
    {
        var instruction = new GcodeInstruction(1, input);

        instruction.Comment.Should().Be(expected);
    }

    [Theory]
    [InlineData("M204 P1000", "M204 P1000")]
    [InlineData("G1 X135.452 Y87.199 E.01116 ;WIDTH:0.449999", "G1 X135.452 Y87.199 E.01116")]
    public void Parses_Command_Text_Correctly(string input, string expected)
    {
        var instruction = new GcodeInstruction(1, input);

        instruction.Command.Should().Be(expected);
    }
}
