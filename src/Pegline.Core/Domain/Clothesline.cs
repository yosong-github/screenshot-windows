namespace Pegline.Core.Domain;

public readonly record struct HangOutcome(HangingItem? Item, IReadOnlyList<HangingItem> Evicted);

/// <summary>
/// What is hanging. Files stay in the capture folder; the line is only the recent set.
/// Single-threaded: the UI session owns the instance.
/// </summary>
public sealed class Clothesline
{
    public const int DefaultCapacity = 8;

    private readonly List<HangingItem> _items = new();
    private readonly Func<double> _tilt;
    private int _max = DefaultCapacity;

    public Clothesline(Func<double>? tilt = null)
    {
        _tilt = tilt ?? DefaultTilt;
    }

    public int MaxItems
    {
        get => _max;
        set => _max = Math.Max(1, value);
    }

    public IReadOnlyList<HangingItem> Items => _items;

    public int Count => _items.Count;

    public HangingItem? Find(Guid id) => _items.Find(item => item.Id == id);

    public HangOutcome Hang(string path, int pixelWidth, int pixelHeight)
    {
        if (string.IsNullOrWhiteSpace(path) || _items.Exists(item => PathsEqual(item.Path, path)))
        {
            return new HangOutcome(null, Array.Empty<HangingItem>());
        }

        var tilt = Math.Clamp(_tilt(), LineLayout.MinTilt, LineLayout.MaxTilt);
        var item = new HangingItem(Guid.NewGuid(), path, pixelWidth, pixelHeight, tilt);
        _items.Add(item);
        return new HangOutcome(item, TrimOverflow());
    }

    public HangingItem? TakeDown(Guid id)
    {
        var index = _items.FindIndex(item => item.Id == id);
        if (index < 0)
        {
            return null;
        }

        var item = _items[index];
        _items.RemoveAt(index);
        return item;
    }

    /// <summary>
    /// Puts saved shots back. Oldest entries past <see cref="MaxItems"/> leave the line
    /// and stay on disk; nothing is deleted here.
    /// </summary>
    public IReadOnlyList<HangingItem> Restore(IEnumerable<HangingItem> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Path) || _items.Exists(existing => PathsEqual(existing.Path, item.Path)))
            {
                continue;
            }

            _items.Add(item);
        }

        return TrimOverflow();
    }

    public static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private List<HangingItem> TrimOverflow()
    {
        var removed = new List<HangingItem>();
        while (_items.Count > _max)
        {
            removed.Add(_items[0]);
            _items.RemoveAt(0);
        }

        return removed;
    }

    private static double DefaultTilt() =>
        Random.Shared.NextDouble() * (LineLayout.MaxTilt - LineLayout.MinTilt) + LineLayout.MinTilt;
}
