using System;
using System.IO;
using System.Linq;

namespace SliceyDicey.Tests;

public static class TestHelper
{
    public static Stream ReadEmbeddedResource(string filename, string type = "input")
    {
        var assembly = typeof(TestHelper).Assembly;
        var resource = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith($"{type}.{filename}"));
        if (string.IsNullOrWhiteSpace(resource))
            throw new InvalidOperationException($"Unable to find resource with name {filename}");

        return assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"Unable to find resource {filename}");
    } 
}
