using System.Collections;

namespace Ogma;

public sealed class Store<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private readonly StoreOptions             _options;
    private readonly Dictionary<TKey, TValue> _kvs;

    public TValue this[ TKey key ]
    {
        get => this._kvs[key];
        set => this._kvs[key] = value;
    }

    public IReadOnlyCollection<TKey>   Keys    => this._kvs.Keys;
    public IReadOnlyCollection<TValue> Values  => this._kvs.Values;
    public int                         Count   => this._kvs.Count;
    public bool                        IsEmpty => this._kvs.Count == 0;

    public Store( StoreOptions options )
    {
        this._kvs     = new();
        this._options = options;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public Store( IEnumerable<KeyValuePair<TKey, TValue>> kvs, StoreOptions options )
    {
        this._kvs     = kvs.ToDictionary( x => x.Key, x => x.Value );
        this._options = options;
    }

    public void Clear()            => this._kvs.Clear();
    public void Remove( TKey key ) => this._kvs.Remove( key );

    public bool ContainsKey( TKey key ) => this._kvs.ContainsKey( key );

    public bool TryGetValue( TKey key, out TValue? value ) => this._kvs.TryGetValue( key, out value );

    public bool ContainsValue( TValue value ) => this._kvs.ContainsValue( value );

    public bool TryAdd( TKey key, TValue value ) => this._kvs.TryAdd( key, value );

    public TValue GetOrAdd( TKey key, Func<TKey, TValue> valueFactory )
    {
        if( this._kvs.TryGetValue( key, out var existing ) )
            return existing;

        var created = valueFactory( key );
        this._kvs[key] = created;
        return created;
    }

    public ValueTask SaveAsync( CancellationToken cancellationToken = default )
    {
        var records = this._kvs.Select( r => new Record<TKey, TValue>( r.Key, r.Value ) );
        var doc     = new Document<TKey, TValue> { Store = [..records] };

        return FileFormat.WriteAsync( doc, this._options, cancellationToken );
    }

    public static async ValueTask<Store<TKey, TValue>> OpenAsync(
        StoreOptions      options           = default,
        CancellationToken cancellationToken = default
    )
    {
        await using var file    = File.Open( options.Path, FileMode.Open, FileAccess.Read, FileShare.Read );
        var             doc     = await FileFormat.ReadAsync<TKey, TValue>( options, cancellationToken );
        var             records = doc.Store.Select( r => new KeyValuePair<TKey, TValue>( r.Key, r.Value ) );
        var             store   = new Store<TKey, TValue>( records, options );

        return store;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this._kvs.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ( (IEnumerable)this._kvs ).GetEnumerator();
}
