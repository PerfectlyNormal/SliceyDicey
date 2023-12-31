﻿using System.Text;

namespace SliceyDicey.Lib.Gcode;

public static class GcodeParser
{
    public static async Task<GcodeFile> Parse(string name, Stream input, CancellationToken cancellationToken)
    {
        var instructions = new List<GcodeInstruction>();
        var thumbnails = new List<IGcodeThumbnail>();

        using var reader = new StreamReader(input, Encoding.UTF8);
        var lineNo = 0;
        var thumbnail = (IGcodeThumbnail?)null;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNo += 1;
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith(GcodePngThumbnail.Prefix))
            {
                var parts = line.Split(' ');
                thumbnail = new GcodePngThumbnail(parts[3], long.Parse(parts[4]));
            }
            else if (line.StartsWith(GcodeQoiThumbnail.Prefix))
            {
                var parts = line.Split(' ');
                thumbnail = new GcodeQoiThumbnail(parts[3], long.Parse(parts[4]));
            }
            else if (thumbnail is not null && (line.StartsWith(GcodeQoiThumbnail.Suffix) || line.StartsWith(GcodePngThumbnail.Suffix)))
            {
                thumbnails.Add(thumbnail);
                thumbnail = null;
            }
            else if (thumbnail is not null)
            {
                var data = line[2..];
                thumbnail.AddLine(data);
            }
            else
            {
                instructions.Add(new GcodeInstruction(lineNo, line));
            }
        }

        return new GcodeFile(name, instructions, thumbnails);
    }
}
