namespace SliceyDicey.Lib.Gcode;

public class GcodePngThumbnail : IGcodeThumbnail
{
    public const string Prefix = "; thumbnail begin";
    public const string Suffix = "; thumbnail end";

    public GcodePngThumbnail(string dimensions, long size)
    {
        Dimensions = dimensions;
        Size = size;
        Encoded = "";
    }
    
    public GcodePngThumbnail(string dimensions, long size, Span<byte> data) : this(dimensions, size)
    {
        _data = data.ToArray();
    }

    private byte[]? _data;

    public string Format => "PNG";
    public string Dimensions { get; }
    public long Size { get; }
    
    public void AddLine(string data)
    {
        Encoded += data.Trim();
    }

    public Stream GeneratePng()
        => new MemoryStream(Data);

    private string Encoded { get; set; }

    private byte[] Data
    {
        get
        {
            _data ??= Convert.FromBase64String(Encoded);
            return _data;
        }
    }
}