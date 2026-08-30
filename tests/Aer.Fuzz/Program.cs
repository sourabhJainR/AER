using System.Text.Json;
using Aer;

var random = new Random(0xAER);
const int cases = 2000;

for (var i = 0; i < cases; i++)
{
    var json = RandomValue(random, depth: 0);
    var value = AerValue.FromJson(json);
    var text = AER.Serialize(value);
    var textRoundTrip = AER.Deserialize(text).ToJsonElement();
    var binary = AER.ToBinary(value);
    var binaryRoundTrip = AER.FromBinary(binary).ToJsonElement();

    if (!JsonElement.DeepEquals(json, textRoundTrip))
        throw new InvalidOperationException($"Text property failure at case {i}: {text}");
    if (!JsonElement.DeepEquals(json, binaryRoundTrip))
        throw new InvalidOperationException($"Binary property failure at case {i}.");
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
        3 => JsonSerializer.SerializeToElement(Enumerable.Range(0, random.Next(0, 6)).ToDictionary(_ => "k" + random.Next(100), _ => RandomScalar(random))),
        4 => JsonSerializer.SerializeToElement(Enumerable.Range(0, random.Next(0, 5)).Select(_ => new Dictionary<string, object?>
        {
            ["id"] = random.Next(10000),
            ["name"] = "n" + random.Next(1000),
            ["active"] = random.Next(2) == 1
        }).ToArray()),
        _ => RandomNested(random, depth + 1)
    };
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
