# SliceyDicey

A simple tool for parsing both ASCII-based and binary GCode files

Only reading for now, I have no need for writing my own files.

## Reading ASCII-based GCode

```csharp
// Arrange
string name = "some-random.gcode";
Stream input = new MemoryStream();

// Act
var file = await SliceyDicey.Lib.Gcode.GcodeParser.Parse(name, input, cancellationToken);

// Play around
var thumbnails = file.Thumbnails.ToList();
var filamentUsed = file.Comments
    .FirstOrDefault(x => x.Comment.StartsWith("filament used [mm] ="));
var linearMoves = file.Commands
    .Where(x => x.Command.StartsWith("G0 ") || x.Command.StartsWith("G1 "));
```

## Reading binary GCode

```csharp
// Arrange
Microsoft.Extensions.Logging.ILogger logger = null;
string name = "some-random.gcode";
Stream input = new MemoryStream();

// Act
var parser = new SliceyDicey.Lib.BinaryGcode.Parser(logger);
var file = await parser.Parse(name, input, cancellationToken);

// Play around
var thumbnails = file.Thumbnails.ToList();
var allInstructions = file.Instructions.ToList();
var comments = file.Comments.ToList();
var commands = file.Commands.ToList();

if (file.PrinterMetadata.TryGetValue("filament used [mm]", out var filamentUsed))
{
    logger.LogInformation("Using {0:.2f} mm of filament", decimal.Parse(filamentUsed));
}
else
{
    logger.LogWarning("File contains no filament usage information");
}

var linearMoves = file.Commands
    .Where(x => x.Command.StartsWith("G0 ") || x.Command.StartsWith("G1 "));
```

