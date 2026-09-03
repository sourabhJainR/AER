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
    ("array-count-mismatch", () => AER.Deserialize("items[2]: 1")),
    ("unsupported-version", () => AER.Deserialize("@aer 99\na: 1")),
    ("binary-trailing-bytes", () => AerBinary.Decode(AerBinary.Encode(AerValue.Int(1)).Concat(new byte[] { 0xFF }).ToArray())),
    ("binary-invalid-bool", () => AerBinary.Decode(new byte[] { (byte)'A', (byte)'E', (byte)'R', (byte)'B', 1, (byte)AerKind.Bool, 2 })),
};

foreach (var (id, test) in negative)
{
    try { test(); Console.Error.WriteLine($"FAIL {id}: malformed input was accepted"); failures++; }
    catch (AerFormatException) { Console.WriteLine($"PASS {id}"); }
}

var emptyRoot = AerValue.Object(new Dictionary<string, AerValue>());
var emptyRootText = AER.Serialize(emptyRoot);
if (!string.Equals(emptyRootText, "{}\n", StringComparison.Ordinal))
{
    Console.Error.WriteLine($"FAIL empty-root-object: unexpected encoding '{emptyRootText}'");
    failures++;
}
else
{
    var emptyRootRoundTrip = AER.Deserialize(emptyRootText);
    if (emptyRootRoundTrip.Kind != AerKind.Object || ((IReadOnlyDictionary<string, AerValue>)emptyRootRoundTrip.Data!).Count != 0)
    {
        Console.Error.WriteLine("FAIL empty-root-object: roundtrip changed the root value");
        failures++;
    }
    else Console.WriteLine("PASS empty-root-object");
}

return failures == 0 ? 0 : 1;
