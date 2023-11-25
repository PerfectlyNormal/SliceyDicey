namespace SliceyDicey.Lib.Gcode;

public class GcodeFile
{
    public GcodeFile(string name, List<GcodeInstruction> instructions, List<IGcodeThumbnail> thumbnails)
    {
        Name = name;
        Lines = instructions;
        Thumbnails = thumbnails;
    }

    public string Name { get; set; }
    public IEnumerable<GcodeInstruction> Lines { get; set; }
    public IEnumerable<IGcodeThumbnail> Thumbnails { get; set; }
    public IEnumerable<GcodeInstruction> Commands => Lines.Where(x => x.HasCommand).OrderBy(x => x.LineNo);
    public IEnumerable<GcodeInstruction> Comments => Lines.Where(x => x.HasComment && !x.HasCommand).OrderBy(x => x.LineNo);
}
