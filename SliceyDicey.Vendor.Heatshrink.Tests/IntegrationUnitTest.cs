using System.Text;
using SliceyDicey.Lib.Heatshrink;
using Xunit;

namespace SliceyDicey.Vendor.Heatshrink.Tests;

// (c) 2019 coverxit (https://github.com/coverxit)
// Published under the LGPL-3.0 license
// https://github.com/coverxit/HeatshrinkDotNet/tree/master

public class IntegrationUnitTest
{
    [Fact]
    public void DataWithoutDuplicationShouldMatch()
    {
        var input = Enumerable.Range('a', 'z' - 'a' + 1).Select(c => (byte)c).ToArray();
        var cfg = new ConfigInfo
        {
            LogLevel = 0,
            WindowSz = 8,
            LookaheadSz = 3,
            DecoderInputBufferSize = 256
        };
        Helper.CompressAndExpandAndCheck(input, cfg);
    }

    [Fact]
    public void DataWithSimpleRepetitionShouldCompressAndDecompressProperly()
    {
        var input = Encoding.UTF8.GetBytes("abcabcdabcdeabcdefabcdefgabcdefgh");
        var cfg = new ConfigInfo
        {
            LogLevel = 0,
            WindowSz = 8,
            LookaheadSz = 3,
            DecoderInputBufferSize = 256
        };
        Helper.CompressAndExpandAndCheck(input, cfg);
    }

    [Fact]
    public void DataWithoutDuplicationShouldMatchWithAbsurdlyTinyBuffer()
    {
        var encoder = new HeatshrinkEncoder(8, 3);
        var decoder = new HeatshrinkDecoder(256, 8, 3);
        var input = Enumerable.Range('a', 'z' - 'a' + 1).Select(c => (byte)c).ToArray();
        var comp = new byte[60];
        var decomp = new byte[60];
        var log = false;

        if (log) Helper.DumpBuf("input", input);
        for (var i = 0; i < input.Length; ++i)
            Assert.True(encoder.Sink(input, i, 1, out var count) >= 0);
        Assert.Equal(EncoderFinishResult.More, encoder.Finish());

        var packedCount = 0;
        do
        {
            Assert.True(encoder.Poll(comp, packedCount, 1, out var count) >= 0);
            packedCount += count;
        } while (encoder.Finish() == EncoderFinishResult.More);

        if (log) Helper.DumpBuf("comp", comp, packedCount);
        for (var i = 0; i < packedCount; ++i)
            Assert.True(decoder.Sink(comp, i, 1, out var count) >= 0);

        for (var i = 0; i < input.Length; ++i)
            Assert.True(decoder.Poll(decomp, i, 1, out var count) >= 0);

        if (log) Helper.DumpBuf("decomp", decomp, input.Length);
        for (var i = 0; i < input.Length; ++i)
            Assert.Equal(input[i], decomp[i]);
    }

    [Fact]
    public void DataWithSimpleRepetitionShouldMatchWithAbsurdlyTinyBuffers()
    {
        var encoder = new HeatshrinkEncoder(8, 3);
        var decoder = new HeatshrinkDecoder(256, 8, 3);
        var input = Encoding.UTF8.GetBytes("abcabcdabcdeabcdefabcdefgabcdefgh");
        var comp = new byte[60];
        var decomp = new byte[60];
        var log = false;

        if (log) Helper.DumpBuf("input", input);
        for (var i = 0; i < input.Length; ++i)
            Assert.True(encoder.Sink(input, i, 1, out var count) >= 0);
        Assert.Equal(EncoderFinishResult.More, encoder.Finish());

        var packedCount = 0;
        do
        {
            Assert.True(encoder.Poll(comp, packedCount, 1, out var count) >= 0);
            packedCount += count;
        } while (encoder.Finish() == EncoderFinishResult.More);

        if (log) Helper.DumpBuf("comp", comp, packedCount);
        for (var i = 0; i < packedCount; ++i)
            Assert.True(decoder.Sink(comp, i, 1, out var count) >= 0);

        for (var i = 0; i < input.Length; ++i)
            Assert.True(decoder.Poll(decomp, i, 1, out var count) >= 0);

        if (log) Helper.DumpBuf("decomp", decomp, input.Length);
        for (var i = 0; i < input.Length; ++i)
            Assert.Equal(input[i], decomp[i]);
    }

    [Fact]
    public void FuzzingSingleByteSizes()
    {
        Console.WriteLine("Fuzzing (single-byte sizes):");
        for (byte lsize = 3; lsize < 8; lsize++)
        {
            for (ulong size = 1; size < 128 * 1024; size <<= 1)
            {
                Console.WriteLine($" -- size {size}");
                for (ushort ibs = 32; ibs <= 8192; ibs <<= 1) // input buffer size
                {
                    Console.WriteLine($" -- input buffer {ibs}");
                    for (ulong seed = 1; seed <= 10; seed++)
                    {
                        Console.WriteLine($" -- seed {seed}");
                        var cfg = new ConfigInfo
                        {
                            LogLevel = 0,
                            WindowSz = 8,
                            LookaheadSz = lsize,
                            DecoderInputBufferSize = ibs
                        };
                        Helper.PseudoRandomDataShouldMatch(size, seed, cfg);
                    }
                }
            }
        }
    }

    [Fact]
    public void FuzzingMultiByteSizes()
    {
        Console.WriteLine("Fuzzing (multi-byte sizes):");
        for (byte lsize = 6; lsize < 9; lsize++)
        {
            for (ulong size = 1; size < 128 * 1024; size <<= 1)
            {
                Console.WriteLine($" -- size {size}");
                for (ushort ibs = 32; ibs <= 8192; ibs <<= 1) // input buffer size
                {
                    Console.WriteLine($" -- input buffer {ibs}");
                    for (ulong seed = 1; seed <= 10; seed++)
                    {
                        Console.WriteLine($" -- seed {seed}");
                        var cfg = new ConfigInfo
                        {
                            LogLevel = 0,
                            WindowSz = 11,
                            LookaheadSz = lsize,
                            DecoderInputBufferSize = ibs
                        };
                        Helper.PseudoRandomDataShouldMatch(size, seed, cfg);
                    }
                }
            }
        }
    }
}
