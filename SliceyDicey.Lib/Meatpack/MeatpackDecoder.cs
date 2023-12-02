using System.Text;

namespace SliceyDicey.Lib.Meatpack;

public class MeatpackDecoder
{
    /// <summary>
    /// <see langword="true" /> if we are supposed to un-binarize
    /// the characters. If we are in a comment, we gain nothing from
    /// having to encode most all characters, so toggle how we treat
    /// characters using the <see cref="MeatpackConstants.EnablePacking"/>
    /// and <see cref="MeatpackConstants.DisablePacking"/> commands. 
    /// </summary>
    private bool _unbinarizing;
    
    /// <summary>
    /// <see langword="true"/> if spaces are stripped and we
    /// want to use an 'E' instead. Can be toggled on and off
    /// using the <see cref="MeatpackConstants.EnableNoSpaces"/>
    /// and <see cref="MeatpackConstants.DisableNoSpaces"/> commands. 
    /// </summary>
    private bool _nospaceEnabled;
    
    /// <summary>
    /// <see langword="true"/> if a command is pending
    /// </summary>
    private bool _cmdActive;

    /// <summary>
    /// Buffers a character if dealing with out-of-sequence pairs
    /// </summary>
    private byte _charBuf;

    /// <summary>
    /// Counts how many command bytes are received.
    ///
    /// <para>We need two command bytes in a row to enable command mode</para>
    /// </summary>
    private int _cmdCount;

    /// <summary>
    /// Counts how many full-width characters are to be received
    /// </summary>
    private int _fullCharQueue;

    /// <summary>
    /// Stores number of characters to be read out
    /// </summary>
    private int _charOutCount;

    /// <summary>
    /// Output buffer for caching up to 2 characters
    /// </summary>
    private readonly List<uint> _charOutBuf = new(2);

    /// <summary>
    /// The final buffer we write every decoded character to
    /// </summary>
    private readonly List<byte> _unbinBuffer = new();

    public string Unbinarize(IEnumerable<byte> src)
    {
        var addSpace = false;
        foreach (var cBin in src)
        {
            if (cBin == MeatpackConstants.SignalByte)
            {
                if (_cmdCount > 0)
                {
                    _cmdActive = true;
                    _cmdCount = 0;
                }
                else
                {
                    _cmdCount++;
                }
            }
            else
            {
                if (_cmdActive)
                {
                    HandleCommand(cBin);
                    _cmdActive = false;
                }
                else
                {
                    if (_cmdCount > 0)
                    {
                        HandleRxChar(MeatpackConstants.SignalByte);
                        _cmdCount = 0;
                    }

                    HandleRxChar(cBin);
                }
            }

            if (cBin == (byte)'\n')
            {
                addSpace = false;
            }

            var cUnbin = new List<char>();
            var charCount = GetResultChar(cUnbin);
            for (var i = 0; i < charCount; i++)
            {
                var item = cUnbin[i];

                addSpace = item switch
                {
                    'G' when _unbinBuffer.Count == 0 || _unbinBuffer.Last() == (byte)'\n' => true,
                    '\n' => false,
                    _ => addSpace
                };

                if (addSpace && (_unbinBuffer.Count == 0 || _unbinBuffer.Last() != (byte)' ') &&
                    IsGLineParameter(item))
                {
                    _unbinBuffer.Add((byte)' ');
                }

                if (item != '\n' || _unbinBuffer.Count == 0 || _unbinBuffer.Last() != (byte)'\n')
                {
                    _unbinBuffer.Add((byte)item);
                }
            }
        }

        return Encoding.ASCII.GetString(_unbinBuffer.ToArray());
    }

    private void HandleCommand(uint c)
    {
        switch (c)
        {
            case MeatpackConstants.EnablePacking:
                _unbinarizing = true;
                break;
            case MeatpackConstants.DisablePacking:
                _unbinarizing = false;
                break;
            case MeatpackConstants.EnableNoSpaces:
                _nospaceEnabled = true;
                break;
            case MeatpackConstants.DisableNoSpaces:
                _nospaceEnabled = false;
                break;
            case MeatpackConstants.ResetAll:
                _unbinarizing = false;
                break;
        }
    }

    private static byte GetChar(uint c, bool noSpace)
    {
        var result = c switch
        {
            0b0000 => '0',
            0b0001 => '1',
            0b0010 => '2',
            0b0011 => '3',
            0b0100 => '4',
            0b0101 => '5',
            0b0110 => '6',
            0b0111 => '7',
            0b1000 => '8',
            0b1001 => '9',
            0b1010 => '.',
            0b1011 => noSpace ? 'E' : ' ',
            0b1100 => '\n',
            0b1101 => 'G',
            0b1110 => 'X',
            _ => '\0'
        };

        return (byte)result;
    }

    private uint UnpackChars(uint pk, byte[] charsOut)
    {
        uint output = 0;

        // If lower 4 bytes is 0b1111, the higher 4 are unused, and next char is full.
        if ((pk & MeatpackConstants.FirstNotPacked) == MeatpackConstants.FirstNotPacked)
            output |= MeatpackConstants.NextPackedFirst;
        else
            charsOut[0] = GetChar(pk & 0xF, _nospaceEnabled); // Assign lower char

        // Check if upper 4 bytes is 0b1111... if so, we don't need the second char.
        if ((pk & MeatpackConstants.SecondNotPacked) == MeatpackConstants.SecondNotPacked)
            output |= MeatpackConstants.NextPackedSecond;
        else
            charsOut[1] = GetChar((pk >> 4) & 0xF, _nospaceEnabled); // Assign upper char

        return output;
    }

    private void HandleRxChar(uint c)
    {
        if (_unbinarizing)
        {
            var buf = new byte[] { 0, 0 };
            var result = UnpackChars(c, buf);
            if (_fullCharQueue > 0)
            {
                HandleOutputChar(c);
                if (_charBuf > 0)
                {
                    HandleOutputChar(_charBuf);
                    _charBuf = 0;
                }

                _fullCharQueue--;
            }
            else
            {
                if ((result & MeatpackConstants.NextPackedFirst) == MeatpackConstants.NextPackedFirst)
                {
                    _fullCharQueue++;
                    if ((result & MeatpackConstants.NextPackedSecond) == MeatpackConstants.NextPackedSecond)
                        _fullCharQueue++;
                    else
                        _charBuf = buf[1];
                }
                else
                {
                    HandleOutputChar(buf[0]);
                    if (buf[0] != (byte)'\n')
                    {
                        if ((result & MeatpackConstants.NextPackedSecond) == MeatpackConstants.NextPackedSecond)
                            _fullCharQueue++;
                        else
                            HandleOutputChar(buf[1]);
                    }
                }
            }
        }
        else
        {
            // Packing not enabled, just copy character to output
            HandleOutputChar(c);
        }

        return;

        void HandleOutputChar(uint c)
        {
            _charOutBuf.Add(c);
            _charOutCount++;
        }
    }

    private int GetResultChar(List<char> charsOut)
    {
        var result = _charOutCount;
        charsOut.AddRange(_charOutBuf.Select(b => (char)b));
        _charOutBuf.Clear();
        _charOutCount = 0;
        return result;
    }

    private static readonly char[] GlineParameters =
    {
        // G0, G1
        'X', 'Y', 'Z', 'E', 'F',
        // G2, G3
        'I', 'J', 'R',
        // G29
        'P', 'W', 'H', 'C', 'A'
    };

    private static bool IsGLineParameter(char c)
        => GlineParameters.Contains(c);
}

internal class MeatpackConstants
{
    internal const uint None = 0x00;

    //TogglePacking = 253 -- Currently unused, byte 253 can be reused later.
    internal const uint EnablePacking = 0xFB; // 251
    internal const uint DisablePacking = 0xFA; // 250
    internal const uint ResetAll = 0xF9; // 249
    internal const uint QueryConfig = 0xF8; // 248
    internal const uint EnableNoSpaces = 0xF7; // 247
    internal const uint DisableNoSpaces = 0xF6; // 246
    internal const uint SignalByte = 0xFF;

    internal const uint SecondNotPacked = 0b11110000;
    internal const uint FirstNotPacked = 0b00001111;
    internal const uint NextPackedFirst = 0b00000001;
    internal const uint NextPackedSecond = 0b00000010;
}
