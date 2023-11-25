using QOI.Core;

namespace SliceyDicey.Lib.Qoi;

public class QoiImageSharpDecoder
{
    private readonly QoiDecoder _qoiDecoder = new();

    public Image Read(Stream stream)
    {
        var qoiImage = _qoiDecoder.Read(stream);
        if (qoiImage.HasAlpha)
            return QoiImageToImage(qoiImage, p => new Rgba32(p.R, p.G, p.B, p.A));

        return QoiImageToImage(qoiImage, p => new Rgb24(p.R, p.G, p.B));
    }

    private static Image<TPixel> QoiImageToImage<TPixel>(QoiImage qoiImage, Func<QoiColor, TPixel> pixelConverter)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        var image = new Image<TPixel>((int)qoiImage.Width, (int)qoiImage.Height);

        image.ProcessPixelRows(pixelAccessor =>
        {
            for (var y = 0; y < qoiImage.Height; y++)
            {
                var rowSpan = pixelAccessor.GetRowSpan(y);
                for (var x = 0; x < qoiImage.Width; x++)
                {
                    var pixelIndex = y * qoiImage.Width + x;
                    rowSpan[x] = pixelConverter(qoiImage.Pixels[pixelIndex]);
                }
            }
        });

        return image;
    }
}