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
            Console.Error.WriteLine($"FAIL {id}: canonical mismatch"); failures++;
        }
        else Console.WriteLine($"PASS {id}");
    }
    catch (Exception ex) { Console.Error.WriteLine($"FAIL {id}: {ex.Message}"); failures++; }
}

var negative = new (string Id, Action Test)[]
{
    ("duplicate-key", () => AER.Deserialize("a: 1\na: 2")),
    ("invalid-base64", () => AER.Deserialize("data: b64\"not-base64!\"")),
    ("unterminated-string", () => AER.Deserialize("name: \"unterminated")),
    ("unsupported-version", () => AER.Deserialize("@aer 99\na: 1")),
    ("binary-trailing-bytes", () => AerBinary.Decode(AerBinary.Encode(AerValue.Int(1)).Concat(new byte[] { 0xFF }).ToArray())),
    ("binary-invalid-bool", () => AerBinary.Decode(new byte[] { (byte)'A', (byte)'E', (byte)'R', (byte)'B', 1, (byte)AerKind.Bool, 2 })),
};

foreach (var (id, test) in negative)
{
    try { test(); Console.Error.WriteLine($"FAIL {id}: malformed input was accepted"); failures++; }
    catch (AerFormatException) { Console.WriteLine($"PASS {id}"); }
}

return failures == 0 ? 0 : 1;
