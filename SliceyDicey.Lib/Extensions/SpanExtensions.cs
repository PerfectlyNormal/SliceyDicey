using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using SliceyDicey.Lib.BinaryGcode;
using SliceyDicey.Lib.Heatshrink;

namespace SliceyDicey.Lib.Extensions;

public static class SpanExtensions
{
    public static Dictionary<string, string> ReadAsIni(this Span<byte> data, int blockSize,
        CompressionType compressionType, ILogger logger)
    {
        if (blockSize < 0 || blockSize > data.Length)
            throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize,
                $"Not enough data passed in. Expected to find at least {blockSize} bytes, but found {data.Length}");

        var text = data.ReadAsText(blockSize, Encoding.ASCII, compressionType, logger);

        return text
            .Split("\n")
            .Select(s =>
            {
                var spl = s.Split('=', 2);
                return spl.Length == 2 ? (spl[0], spl[1]) : (s, "");
            })
            .Select(vt => (vt.Item1.Trim(), vt.Item2.Trim()))
            .Where(vt => vt.Item1 != "")
            .ToDictionary(x => x.Item1, x => x.Item2);
    }

    public static string ReadAsText(this Span<byte> data, int blockSize, Encoding encoding,
        CompressionType compressionType, ILogger logger)
    {
        return compressionType switch
        {
            CompressionType.None => encoding.GetString(data[..blockSize]),
            CompressionType.Deflate => data[..blockSize].DeflateToString(encoding),
            _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType,
                "Unsupported compression type encountered when decoding to text")
        };
    }

    public static byte[] ReadAsBytes(this Span<byte> data, int blockSize, CompressionType compressionType,
        ILogger logger)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return compressionType switch
        {
            CompressionType.None => data[..blockSize].ToArray(),
            CompressionType.Heatshrink11 => data[..blockSize].UnpackHeatshrink(compressionType, logger),
            CompressionType.Heatshrink12 => data[..blockSize].UnpackHeatshrink(compressionType, logger),
            _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType,
                "Unsupported compression type encountered when decoding to text")
        };
    }

    public static byte[] UnpackHeatshrink(this Span<byte> data, CompressionType compressionType, ILogger logger)
    {
        const int bufferSize = 1024;
        logger.LogDebug(
            "Going to unpack Heatshrink using compression {CompressionType}. Got {Bytes} bytes of compressed data",
            compressionType, data.Length);
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        var decoder = compressionType switch
        {
            CompressionType.Heatshrink11 => new HeatshrinkDecoder(bufferSize, 11, 4),
            CompressionType.Heatshrink12 => new HeatshrinkDecoder(bufferSize, 12, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType,
                "Unsupported Heatshrink compression encountered.")
        };

        using var ms = new MemoryStream();
        var finishResult = DecoderFinishResult.Null;
        var bytesRead = 0;
        var output = new byte[bufferSize];
        do
        {
            var end = bytesRead + bufferSize;
            end = Math.Min(end, data.Length);
            var copiedToDecoder = 0;
            if (bytesRead < data.Length)
            {
                logger.LogTrace("Going to read from input, starting at {Position} and reading to {StopPosition}",
                    bytesRead, end);
                var sinkResult = decoder.Sink(data[bytesRead..end].ToArray(), out copiedToDecoder);
                bytesRead += copiedToDecoder;
                logger.LogTrace("Read {count} bytes from input, result is {result}", copiedToDecoder, sinkResult);
            }

            DecoderPollResult pollResult;
            do
            {
                pollResult = decoder.Poll(output, out var outputSize);

                logger.LogTrace("Wrote {count} bytes to output, result is {result}", outputSize, pollResult);
                ms.Write(output, 0, outputSize);
            } while (pollResult != DecoderPollResult.Empty);

            if (copiedToDecoder < bufferSize)
            {
                finishResult = decoder.Finish();
                logger.LogTrace("No more input. Letting decoder know we're done. Answer was {FinishResult}",
                    finishResult);
            }
        } while (finishResult != DecoderFinishResult.Done);

        return ms.ToArray();
    }

    public static Span<byte> Concat(this Span<byte> span0, Span<byte> span1)
    {
        var result = new byte[span0.Length + span1.Length];
        var resultSpan = result.AsSpan();
        span0.CopyTo(result);

        var from = span0.Length;
        span1.CopyTo(resultSpan[from..]);

        return result.AsSpan();
    }

    private static string DeflateToString(this Span<byte> data, Encoding? encoding = null)
    {
        using var ms = new MemoryStream(data.ToArray());
        using var stream = new ZLibStream(ms, CompressionMode.Decompress);

        var output = new MemoryStream();
        stream.CopyTo(output);

        return (encoding ?? Encoding.ASCII).GetString(output.ToArray());
    }
}