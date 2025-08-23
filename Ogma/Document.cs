using System.Collections.Immutable;

namespace Ogma;

internal sealed record Document<TKey, TValue>
{
    // used for serialization and deserialization.
    // for now the records are the only property, but other properties can be added later
    // if needed, e.g. metadata
    public ImmutableArray<Record<TKey, TValue>> Store { get; init; }
}
