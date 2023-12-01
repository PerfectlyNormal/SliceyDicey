namespace SliceyDicey.Lib.Heatshrink;

// (c) 2019 coverxit (https://github.com/coverxit)
// Published under the LGPL-3.0 license
// https://github.com/coverxit/HeatshrinkDotNet/tree/master

public static class Constants
{
    public const bool EnableLogging = false;

    public const int MinWindowBits = 4;
    public const int MaxWindowBits = 15;

    public const int MinLookaheadBits = 3;

    public const byte LiteralMarker = 0x01;
    public const byte BackrefMarker = 0x00;
}
