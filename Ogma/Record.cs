namespace Ogma;

internal readonly record struct Record<TKey, TValue>( TKey Key, TValue Value );
