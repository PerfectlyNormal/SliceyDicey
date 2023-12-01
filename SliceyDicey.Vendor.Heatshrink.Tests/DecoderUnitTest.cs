using SliceyDicey.Lib.Heatshrink;
using Xunit;

namespace SliceyDicey.Vendor.Heatshrink.Tests;

// (c) 2019 coverxit (https://github.com/coverxit)
// Published under the LGPL-3.0 license
// https://github.com/coverxit/HeatshrinkDotNet/tree/master

public class DecoderUnitTest
{
    [Fact]
    public void DecoderAllocShouldRejectExcessivelySmallWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HeatshrinkDecoder(256, Constants.MinWindowBits - 1, 4));
    }

    [Fact]
    public void DecoderAllocShouldRejectZeroByteInputBuffer()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HeatshrinkDecoder(0, Constants.MinWindowBits, Constants.MinWindowBits - 1));
    }

    [Fact]
    public void DecoderAllocShouldRejectLookaheadEqualToWindowSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HeatshrinkDecoder(0, Constants.MinWindowBits, Constants.MinWindowBits));
    }

    [Fact]
    public void DecoderAllocShouldRejectLookaheadGreaterThanWindowSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HeatshrinkDecoder(0, Constants.MinWindowBits, Constants.MinWindowBits + 1));
    }

    [Fact]
    public void DecoderSinkShouldRejectExcessivelyLargeInput()
    {
        var input = new byte[] { 0, 1, 2, 3, 4, 5 };
        var decoder = new HeatshrinkDecoder(1, Constants.MinWindowBits, Constants.MinWindowBits - 1);

        // Sink as much as will fit
        var res = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, res);
        Assert.Equal(1, count);

        // And now, no more should fit.
        res = decoder.Sink(input, count, input.Length - count, out count);
        Assert.Equal(DecoderSinkResult.Full, res);
        Assert.Equal(0, count);
    }

    [Fact]
    public void DecoderPollShouldReturnEmptyIfEmpty()
    {
        var output = new byte[256];
        var decoder = new HeatshrinkDecoder(256, Constants.MinWindowBits, Constants.MinWindowBits - 1);

        var res = decoder.Poll(output, out var outSz);
        Assert.Equal(DecoderPollResult.Empty, res);
    }

    [Fact]
    public void DecoderPollShouldExpandShortLiteral()
    {
        var input = new byte[] { 0xb3, 0x5b, 0xed, 0xe0 };
        var output = new byte[4];
        var decoder = new HeatshrinkDecoder(256, 7, 3);

        var sres = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, sres);

        var pres = decoder.Poll(output, out var outSz);
        Assert.Equal(DecoderPollResult.Empty, pres);
        Assert.Equal(3, outSz);
        Assert.Equal((byte)'f', output[0]);
        Assert.Equal((byte)'o', output[1]);
        Assert.Equal((byte)'o', output[2]);
    }

    [Fact]
    public void DecoderPollShouldExpandShortLiteralAndBackref()
    {
        var input = new byte[] { 0xb3, 0x5b, 0xed, 0xe0, 0x41, 0x00 }; // "foofoo"
        var output = new byte[6];
        var decoder = new HeatshrinkDecoder(256, 7, 6);

        var sres = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, sres);

        decoder.Poll(output, out var outSz);

        Assert.Equal(6, outSz);
        Assert.Equal((byte)'f', output[0]);
        Assert.Equal((byte)'o', output[1]);
        Assert.Equal((byte)'o', output[2]);
        Assert.Equal((byte)'f', output[3]);
        Assert.Equal((byte)'o', output[4]);
        Assert.Equal((byte)'o', output[5]);
    }

    [Fact]
    public void DecoderPollShouldExpandShortSelfOverlappingBackref()
    {
        // "aaaaa" == (literal, 1), ('a'), (backref, 1 back, 4 bytes)
        var input = new byte[] { 0xb0, 0x80, 0x01, 0x80 };
        var output = new byte[6];
        var expected = new byte[] { (byte)'a', (byte)'a', (byte)'a', (byte)'a', (byte)'a' };
        var decoder = new HeatshrinkDecoder(256, 8, 7);

        var sres = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, sres);

        decoder.Poll(output, out var outSz);
        Assert.Equal(expected.Length, outSz);
        for (int i = 0; i < expected.Length; ++i) Assert.Equal(expected[i], output[i]);
    }

    [Fact]
    public void DecoderPollShouldSuspendIfOutOfSpaceInOutputBufferDuringLiteralExpansion()
    {
        var input = new byte[] { 0xb3, 0x5b, 0xed, 0xe0, 0x40, 0x80 };
        var output = new byte[1];
        var decoder = new HeatshrinkDecoder(256, 7, 6);

        var sres = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, sres);

        var pres = decoder.Poll(output, out var outSz);
        Assert.Equal(DecoderPollResult.More, pres);
        Assert.Equal(1, outSz);
        Assert.Equal((byte)'f', output[0]);
    }

    [Fact]
    public void DecoderPollShouldSuspendIfOutOfSpaceInOutputBufferDuringBackrefExpansion()
    {
        var input = new byte[] { 0xb3, 0x5b, 0xed, 0xe0, 0x41, 0x00 }; // "foofoo"
        var output = new byte[4];
        var decoder = new HeatshrinkDecoder(256, 7, 6);

        var sres = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, sres);

        var pres = decoder.Poll(output, out var outSz);
        Assert.Equal(DecoderPollResult.More, pres);
        Assert.Equal(4, outSz);
        Assert.Equal((byte)'f', output[0]);
        Assert.Equal((byte)'o', output[1]);
        Assert.Equal((byte)'o', output[2]);
        Assert.Equal((byte)'f', output[3]);
    }

    [Fact]
    public void DecoderPollShouldExpandShortLiteralAndBackrefWhenFedInputByteByByte()
    {
        var input = new byte[] { 0xb3, 0x5b, 0xed, 0xe0, 0x41, 0x00 }; // "foofoo"
        var output = new byte[7];
        var decoder = new HeatshrinkDecoder(256, 7, 6);

        for (var i = 0; i < 6; ++i)
        {
            var sres = decoder.Sink(input, i, 1, out var count);
            Assert.Equal(DecoderSinkResult.Ok, sres);
        }

        var pres = decoder.Poll(output, out var outSz);
        Assert.Equal(DecoderPollResult.Empty, pres);
        Assert.Equal(6, outSz);
        Assert.Equal((byte)'f', output[0]);
        Assert.Equal((byte)'o', output[1]);
        Assert.Equal((byte)'o', output[2]);
        Assert.Equal((byte)'f', output[3]);
        Assert.Equal((byte)'o', output[4]);
        Assert.Equal((byte)'o', output[5]);
    }

    [Fact]
    public void DecoderFinishShouldNoteWhenDoen()
    {
        var input = new byte[] { 0xb3, 0x5b, 0xed, 0xe0, 0x41, 0x00 }; // "foofoo"
        var output = new byte[7];
        var decoder = new HeatshrinkDecoder(256, 7, 6);

        var sres = decoder.Sink(input, out var count);
        Assert.Equal(DecoderSinkResult.Ok, sres);

        var pres = decoder.Poll(output, out var outSz);
        Assert.Equal(DecoderPollResult.Empty, pres);
        Assert.Equal(6, outSz);
        Assert.Equal((byte)'f', output[0]);
        Assert.Equal((byte)'o', output[1]);
        Assert.Equal((byte)'o', output[2]);
        Assert.Equal((byte)'f', output[3]);
        Assert.Equal((byte)'o', output[4]);
        Assert.Equal((byte)'o', output[5]);

        var fres = decoder.Finish();
        Assert.Equal(DecoderFinishResult.Done, fres);
    }

    [Fact]
    public void DecoderShouldNotGetStuckWithFinishYieldingMoreButZeroBytesOutputFromPoll()
    {
        var input = new byte[512];
        for (var i = 0; i < 256; ++i) input[i] = 0xff;

        var decoder = new HeatshrinkDecoder(256, 8, 4);

        /* Confirm that no byte of trailing context can lead to
         * heatshrink_decoder_finish erroneously returning HSDR_FINISH_MORE
         * when heatshrink_decoder_poll will yield 0 bytes.
         *
         * Before 0.3.1, a final byte of 0xFF could potentially cause
         * this to happen, if at exactly the byte boundary. */
        for (int b = 0; b < 256; ++b)
        {
            for (int i = 1; i < 512; ++i)
            {
                input[i] = (byte)b;
                decoder.Reset();

                var output = new byte[1024];
                var sres = decoder.Sink(input, out var count);
                Assert.Equal(DecoderSinkResult.Ok, sres);

                var pres = decoder.Poll(output, out var outSz);
                Assert.Equal(DecoderPollResult.Empty, pres);

                var fres = decoder.Finish();
                Assert.Equal(DecoderFinishResult.Done, fres);

                input[i] = 0xff;
            }
        }
    }
}
