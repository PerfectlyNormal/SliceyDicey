using System.Diagnostics;

namespace SliceyDicey.Lib.Gcode;

[DebuggerDisplay("{LineNo}: {RawData}")]
public class GcodeInstruction
{
    public GcodeInstruction(int lineNo, string rawData)
    {
        LineNo = lineNo;
        RawData = rawData;

        if (rawData.StartsWith(";"))
        {
            Comment = rawData[1..].Trim();
            HasComment = true;
        }
        else
        {
            var parts = rawData.Split(';');
            Command = parts[0].Trim();

            if (parts.Length > 1)
            {
                Comment = rawData[(parts[0].Length + 1)..].Trim();
                HasComment = true;
            }
        }
    }

    public int LineNo { get; set; }
    public string RawData { get; set; }
    public string? Command { get; set; }
    public bool HasCommand => !string.IsNullOrWhiteSpace(Command);
    public string? Comment { get; set; }
    public bool HasComment { get; private set; }
    public bool OnlyComment => !HasCommand && HasComment;
}