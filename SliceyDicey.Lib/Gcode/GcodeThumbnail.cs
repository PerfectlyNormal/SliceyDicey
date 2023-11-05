namespace SliceyDicey.Lib.Gcode;

public class GcodeThumbnail
{
    public const string Prefix = "; thumbnail begin";
    public const string Suffix = "; thumbnail end";

    public GcodeThumbnail(string dimensions, long size)
    {
        Dimensions = dimensions;
        Size = size;
        Encoded = "";
    }

    private byte[]? _data;

    public void AddLine(string data)
    {
        Encoded += data.Trim();
    }

    public string Dimensions { get; }
    public long Size { get; }
    public byte[] Data
    {
        get
        {
            _data ??= Convert.FromBase64String(Encoded);
            return _data;
        }
    }

    private string Encoded { get; set; }
}
