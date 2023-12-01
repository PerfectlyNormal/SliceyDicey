namespace SliceyDicey.Lib.BinaryGcode;

public enum CompressionType
{
    /// <summary>
    /// Uncompressed
    /// </summary>
    None = 0,

    /// <summary>
    /// Deflate
    /// </summary>
    Deflate = 1,

    /// <summary>
    /// Heatshrink algorithm with window size 11 and lookahead size 4
    /// </summary>
    Heatshrink11 = 2,

    /// <summary>
    /// Heatshrink algorithm with window size 12 and lookahead size 4
    /// </summary>
    Heatshrink12 = 3,
}
