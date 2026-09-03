using System.Text.Json;
using Aer;

var random = new Random(0xAEE);
const int cases = 2000;

// AER documents currently have an object root. Exercise the full value space
// inside that valid document envelope so the property suite tests the format
// contract rather than generating unsupported root values.
for (var i = 0; i < cases; i++)
{
    var json = RandomObject(random);
    var value = AerValue.FromJson(json);

    // Validate the canonical text representation is stable after a complete decode/encode cycle.
    var text = AER.Serialize(value);
    var textValue = AER.Deserialize(text);
    var textAgain = AER.Serialize(textValue);
    if (!string.Equals(text, textAgain, StringComparison.Ordinal))
        throw new InvalidOperationException($"Text canonicalization failure at case {i}: {text}");

    // Validate binary and text describe the same canonical AER value.
    var binary = AER.ToBinary(value);
    var binaryValue = AER.FromBinary(binary);
    var binaryAsText = AER.Serialize(binaryValue);
    if (!string.Equals(text, binaryAsText, StringComparison.Ordinal))
        throw new InvalidOperationException($"Binary canonicalization failure at case {i}: {text}");

    // The source JSON is also checked semantically where the AER model has a direct JSON projection.
    var projected = textValue.ToJsonElement();
    if (!JsonElement.DeepEquals(json, projected))
        throw new InvalidOperationException($"Semantic property failure at case {i}: {text}");
}

Console.WriteLine($"AER deterministic property suite passed: {cases} cases.");

static JsonElement RandomValue(Random random, int depth)
{
    if (depth >= 3)
        return JsonSerializer.SerializeToElement(RandomScalar(random));

    return random.Next(6) switch
    {
        0 => JsonSerializer.SerializeToElement((object?)null),
        1 => JsonSerializer.SerializeToElement(RandomScalar(random)),
        2 => JsonSerializer.SerializeToElement(Enumerable.Range(0, random.Next(0, 6)).Select(_ => RandomScalar(random)).ToArray()),
        3 => RandomObject(random),
        4 => JsonSerializer.SerializeToElement(Enumerable.Range(0, random.Next(0, 5)).Select(_ => new Dictionary<string, object?>
        {
            ["id"] = random.Next(10000),
            ["name"] = "n" + random.Next(1000),
            ["active"] = random.Next(2) == 1
        }).ToArray()),
        _ => RandomNested(random, depth + 1)
    };
}

static JsonElement RandomObject(Random random)
{
    var count = random.Next(0, 6);
    var values = new Dictionary<string, object?>(StringComparer.Ordinal);
    for (var i = 0; i < count; i++)
        values["k" + i] = RandomValue(random, 0);
    return JsonSerializer.SerializeToElement(values);
}

static JsonElement RandomNested(Random random, int depth) =>
    JsonSerializer.SerializeToElement(new Dictionary<string, JsonElement>
    {
        ["a"] = RandomValue(random, depth),
        ["b"] = RandomValue(random, depth)
    });

static object RandomScalar(Random random) => random.Next(7) switch
{
    0 => random.Next(-1_000_000, 1_000_000),
    1 => Math.Round(random.NextDouble() * 2000 - 1000, 6),
    2 => random.Next(2) == 1,
    3 => "text-" + random.Next(100000),
    4 => "Unicode-भारत-東京-" + random.Next(1000),
    5 => random.Next(2) == 1 ? 0 : -1,
    _ => ""
};
