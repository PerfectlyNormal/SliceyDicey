using SliceyDicey.Lib.Qoi;

namespace SliceyDicey.Lib.Gcode;

public class GcodeQoiThumbnail : IGcodeThumbnail
{
    public const string Prefix = "; thumbnail_QOI begin";
    public const string Suffix = "; thumbnail_QOI end";

    public GcodeQoiThumbnail(string dimensions, long size)
    {
        Dimensions = dimensions;
        Size = size;
        Encoded = "";
    }

    private byte[]? _data;

    public string Format => "QOI";
    public string Dimensions { get; }
    public long Size { get; }

    public void AddLine(string data)
    {
        Encoded += data.Trim();
    }

    public Stream GeneratePng()
    {
        using var ms = new MemoryStream(Data);
        var image = new QoiImageSharpDecoder().Read(ms);

        var result = new MemoryStream();
        image.SaveAsPng(result);
        return result;
    }

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