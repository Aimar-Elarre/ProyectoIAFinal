
using System.Collections.Generic;

public class Blackboard
{
    readonly Dictionary<string, object> _data = new();

    public void Set<T>(string key, T value)
    {
        _data[key] = value;
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (_data.TryGetValue(key, out object value) && value is T typedValue)
            return typedValue;

        return defaultValue;
    }

    public bool Has(string key)
    {
        return _data.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _data.Remove(key);
    }
}
