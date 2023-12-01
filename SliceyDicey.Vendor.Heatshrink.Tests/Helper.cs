using SliceyDicey.Lib.Heatshrink;
using Xunit;

namespace SliceyDicey.Vendor.Heatshrink.Tests;

// (c) 2019 coverxit (https://github.com/coverxit)
// Published under the LGPL-3.0 license
// https://github.com/coverxit/HeatshrinkDotNet/tree/master

struct ConfigInfo
{
    public int LogLevel;
    public int WindowSz;
    public int LookaheadSz;
    public int DecoderInputBufferSize;
}

static class Helper
{
    internal static void DumpBuf(string name, byte[] buf) => DumpBuf(name, buf, buf.Length);

    internal static void DumpBuf(string name, byte[] buf, int size)
    {
        for (var i = 0; i < size; ++i)
        {
            var c = (char)buf[i];
            Console.WriteLine($"{name} {i}: 0x{buf[i]:x2} ('{(char.IsControl(c) ? '.' : c)}')");
        }
    }

    internal static void CompressAndExpandAndCheck(byte[] input, ConfigInfo cfg)
    {
        var encoder = new HeatshrinkEncoder(cfg.WindowSz, cfg.LookaheadSz);
        var decoder = new HeatshrinkDecoder(cfg.DecoderInputBufferSize, cfg.WindowSz, cfg.LookaheadSz);

        var inputSize = input.Length;
        var compSz = inputSize + (inputSize / 2) + 4;
        var decompSz = inputSize + (inputSize / 2) + 4;

        var comp = new byte[compSz];
        var decomp = new byte[decompSz];

        if (cfg.LogLevel > 1)
        {
            Console.WriteLine("\n^^ COMPRESSING\n");
            DumpBuf("input", input);
        }

        var sunk = 0;
        var polled = 0;
        while (sunk < inputSize)
        {
            var esres = encoder.Sink(input, sunk, inputSize - sunk, out var count);
            Assert.True(esres >= 0);
            sunk += count;
            if (cfg.LogLevel > 1) Console.WriteLine($"^^ sunk {count}");
            if (sunk == inputSize)
                Assert.Equal(EncoderFinishResult.More, encoder.Finish());

            EncoderPollResult pres;
            do
            {
                pres = encoder.Poll(comp, polled, compSz - polled, out count);
                Assert.True(pres >= 0);
                polled += count;
                if (cfg.LogLevel > 1) Console.WriteLine($"^^ polled {count}");
            } while (pres == EncoderPollResult.More);

            Assert.Equal(EncoderPollResult.Empty, pres);
            if (polled >= compSz) Assert.Fail("compression should never expand that much");
            if (sunk == inputSize)
                Assert.Equal(EncoderFinishResult.Done, encoder.Finish());
        }

        if (cfg.LogLevel > 0) Console.Write($"in: {inputSize}, compressed: {polled} ");
        var compressedSize = polled;
        sunk = 0;
        polled = 0;

        if (cfg.LogLevel > 1)
        {
            Console.WriteLine("\n^^ DECOMPRESSING\n");
            DumpBuf("comp", comp, compressedSize);
        }

        while (sunk < compressedSize)
        {
            Assert.True(decoder.Sink(comp, sunk, compressedSize - sunk, out var count) >= 0);
            sunk += count;
            if (cfg.LogLevel > 1) Console.WriteLine($"^^ sunk {count}");
            if (sunk == compressedSize)
                Assert.Equal(DecoderFinishResult.More, decoder.Finish());

            DecoderPollResult pres;
            do
            {
                pres = decoder.Poll(decomp, polled, decompSz - polled, out count);
                Assert.True(pres >= 0);
                Assert.True(count > 0);
                polled += count;
                if (cfg.LogLevel > 1) Console.WriteLine($"^^ polled: {count}");
            } while (pres == DecoderPollResult.More);

            Assert.Equal(DecoderPollResult.Empty, pres);
            if (sunk == compressedSize)
            {
                var fres = decoder.Finish();
                Assert.Equal(DecoderFinishResult.Done, fres);
            }

            if (polled > inputSize)
            {
                Console.WriteLine($"\nExpected: {inputSize}, got {polled}\n");
                Assert.Fail("Decompressed data is larger than original input");
            }
        }

        if (cfg.LogLevel > 0) Console.WriteLine($"decompressed: {polled}");
        if (polled != inputSize)
            Assert.Fail("Decompressed length does not match original input length");

        if (cfg.LogLevel > 1) DumpBuf("decomp", decomp, polled);
        for (var i = 0; i < inputSize; ++i)
        {
            if (input[i] != decomp[i])
                Console.WriteLine($"*** mismatch at {i}");
            Assert.Equal(input[i], decomp[i]);
        }
    }

    internal static void FillWithPseudoRandomLetters(byte[] buffer, ulong seed)
    {
        ulong rn = 9223372036854775783; /* prime under 2^64 */
        for (int i = 0; i < buffer.Length; ++i)
        {
            rn = rn * seed + seed;
            buffer[i] = (byte)((rn % 26) + 'a');
        }
    }

    internal static void PseudoRandomDataShouldMatch(ulong size, ulong seed, ConfigInfo cfg)
    {
        var input = new byte[size];
        if (cfg.LogLevel > 0)
            Console.WriteLine($"\n-- size {size}, seed {seed}, input buf {cfg.DecoderInputBufferSize}\n");

        FillWithPseudoRandomLetters(input, seed);
        CompressAndExpandAndCheck(input, cfg);
    }
}
