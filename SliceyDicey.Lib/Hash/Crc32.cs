using Microsoft.Extensions.Logging;

namespace SliceyDicey.Lib.Hash;

public static class Crc32
{
    private const uint CrcMagic = 0xEDB88320;

    public static uint Hash(IEnumerable<byte> data, uint crc, ILogger? logger)
    {
        logger?.LogTrace("Appending {Data} size {Length} to checksum, started with {OldCrc}",
            data.ToArray().Select(x => x.ToString("X2")), data.Count(),
            string.Join("", BitConverter.GetBytes(crc).Select(x => x.ToString("x2"))));

        var value = crc ^ uint.MaxValue;

        foreach (var item in data)
        {
            value ^= item;
            for (var bit = 0; bit < 8; bit++)
            {
                if ((value & 1) == 1)
                    value = (value >> 1) ^ CrcMagic;
                else
                    value >>= 1;
            }
        }

        return value ^ uint.MaxValue;
    }
}
