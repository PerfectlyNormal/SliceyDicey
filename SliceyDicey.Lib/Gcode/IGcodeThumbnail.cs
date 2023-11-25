namespace SliceyDicey.Lib.Gcode;

public interface IGcodeThumbnail
{
    string Format { get; }
    string Dimensions { get; }
    long Size { get; }

    void AddLine(string data);
    Stream GeneratePng();
}