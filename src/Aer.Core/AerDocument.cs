namespace Aer;

public sealed record AerTable(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<AerValue>> Rows)
{
    public AerTable Validate()
    {
        if (Columns.Count == 0) throw new FormatException("AER table must have at least one column.");
        for (var i = 0; i < Rows.Count; i++)
            if (Rows[i].Count != Columns.Count)
                throw new FormatException($"Row {i} has {Rows[i].Count} cells; expected {Columns.Count}.");
        return this;
    }
}

public sealed record AerDocument(
    int Version,
    AerValue Root,
    IReadOnlyDictionary<string, string>? Directives = null,
    IReadOnlyDictionary<string, AerSchema>? Schemas = null)
{
    public static AerDocument Create(AerValue root) => new(1, root);
}
