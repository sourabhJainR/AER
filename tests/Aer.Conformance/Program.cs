using System.Text.Json;
using Aer;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var directory = Path.Combine(root, "conformance", "valid");
var files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
var failures = 0;

foreach (var file in files)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(file));
    var test = doc.RootElement;
    var id = test.GetProperty("id").GetString() ?? Path.GetFileName(file);
    var text = test.GetProperty("text").GetString() ?? string.Empty;
    var expected = test.GetProperty("canonical").GetRawText();

    try
    {
        var value = AER.Deserialize(text);
        var actual = value.ToJsonElement().GetRawText();
        using var expectedDoc = JsonDocument.Parse(expected);
        using var actualDoc = JsonDocument.Parse(actual);
        if (!JsonElement.DeepEquals(expectedDoc.RootElement, actualDoc.RootElement))
        {
            Console.Error.WriteLine($"FAIL {id}: canonical mismatch");
            failures++;
        }
        else
        {
            Console.WriteLine($"PASS {id}");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {id}: {ex.Message}");
        failures++;
    }
}

return failures == 0 ? 0 : 1;
