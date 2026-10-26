using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ScreenshotWindows.App.Imaging;
using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.App.ViewModels;

public partial class LineViewModel : ObservableObject
{
    private double _width;
    private double _ropeTop = LineLayout.RopeTop;
    private double _cacheScale = 1;

    public ObservableCollection<HangingCardViewModel> Cards { get; } = new();

    [ObservableProperty] private bool _isRevealed;

    [ObservableProperty] private bool _showHint = true;

    [ObservableProperty] private string _captureShortcut = HotkeyChord.CaptureDefault.ToDisplay();

    public event EventHandler<Guid>? CopyRequested;

    public event EventHandler<Guid>? RemoveRequested;

    public void Replace(IReadOnlyList<HangingItem> items, Guid? flyingId, ImageSource? flyingImage = null)
    {
        var previous = Cards.ToDictionary(card => card.Id);
        Cards.Clear();
        foreach (var item in items)
        {
            if (!previous.TryGetValue(item.Id, out var card))
            {
                var image = item.Id == flyingId && flyingImage is not null
                    ? flyingImage
                    : ImageLoader.FromFile(item.Path, 480);
                card = new HangingCardViewModel(item, image);
                card.CacheScale = _cacheScale;
            }

            card.Flying = flyingId == item.Id;
            Cards.Add(card);
        }

        ShowHint = Cards.Count == 0;
        Layout(_width);
    }

    public HangingCardViewModel? Find(Guid id) => Cards.FirstOrDefault(card => card.Id == id);

    public void Layout(double width, double ropeTop = LineLayout.RopeTop)
    {
        if (width > 0)
        {
            _width = width;
        }

        _ropeTop = ropeTop;
        var count = Cards.Count;
        for (var index = 0; index < count; index++)
        {
            var card = Cards[index];
            var x = LineLayout.ItemX(index, count, _width, LineLayout.Spacing);
            var photo = LineLayout.PhotoSize(card.PixelWidth, card.PixelHeight);
            card.PhotoWidth = photo.Width;
            card.PhotoHeight = photo.Height;
            var frameExtra = (LineLayout.FrameInset + LineLayout.FrameBorder) * 2;
            card.ChromeWidth = photo.Width + frameExtra;
            card.FrameHeight = photo.Height + frameExtra;
            card.ChromeHeight = LineLayout.FrameTop + card.FrameHeight;
            card.ClipLeft = (card.ChromeWidth - LineLayout.ClipWidth) / 2;
            card.FrameTop = LineLayout.FrameTop;
            card.PivotX = card.ChromeWidth / 2;
            card.PivotY = LineLayout.ClipOverhang;
            var left = x - card.ChromeWidth / 2;
            var top = LineLayout.CardTop(LineLayout.RopeY(x, _width, _ropeTop));
            if (card.Placed && !card.Flying)
            {
                card.ShiftKickX += card.Left - left;
                card.ShiftKickY += card.Top - top;
            }

            card.Left = left;
            card.Top = top;
            card.Placed = true;
            var edge = Math.Min(x, _width - x);
            card.Opacity = card.Flying ? 0 : Math.Clamp(edge / 80, 0, 1);
        }
    }

    public void MarkCopied(Guid id)
    {
        foreach (var card in Cards)
        {
            card.Copied = card.Id == id;
        }
    }

    public void ClearCopied(Guid id)
    {
        var card = Find(id);
        if (card is not null)
        {
            card.Copied = false;
        }
    }

    public void MarkLanded(Guid id)
    {
        var card = Find(id);
        if (card is null)
        {
            return;
        }

        card.Flying = false;
        card.SwingArm = false;
        card.SwingKick = Motion.LandingSwingVelocity;
        Layout(_width, _ropeTop);
    }

    public void SetCacheScale(double scale)
    {
        _cacheScale = scale <= 0 ? 1 : scale;
        foreach (var card in Cards)
        {
            card.CacheScale = _cacheScale;
        }
    }

    public void AnimateAccent(Guid id, int? rgb)
    {
        Find(id)?.SetAccent(rgb, animate: true);
    }

    public void RequestCopy(Guid id) => CopyRequested?.Invoke(this, id);

    public void RequestRemove(Guid id) => RemoveRequested?.Invoke(this, id);
}
