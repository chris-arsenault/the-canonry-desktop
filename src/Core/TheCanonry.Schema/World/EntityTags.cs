namespace TheCanonry.Schema.World;

public class EntityTags
{
    private readonly Dictionary<string, object> _tags = [];

    public void Set(string key, string value) => _tags[key] = value;
    public void Set(string key, bool value) => _tags[key] = value;
    public void Remove(string key) => _tags.Remove(key);
    public bool Contains(string key) => _tags.ContainsKey(key);

    public string? GetString(string key) =>
        _tags.TryGetValue(key, out var val) && val is string s ? s : null;

    public bool GetBool(string key) =>
        _tags.TryGetValue(key, out var val) && val is true;

    public IReadOnlyDictionary<string, object> All => _tags;

    public EntityTags Clone()
    {
        var clone = new EntityTags();
        foreach (var (key, value) in _tags)
            clone._tags[key] = value;
        return clone;
    }
}
