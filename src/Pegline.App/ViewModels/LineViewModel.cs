using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Pegline.App.Imaging;
using Pegline.Core.Domain;

namespace Pegline.App.ViewModels;

public partial class LineViewModel : ObservableObject
{
    private double _width;

    public ObservableCollection<HangingCardViewModel> Cards { get; } = new();

    [ObservableProperty] private bool _isRevealed;

    [ObservableProperty] private bool _showHint = true;

    [ObservableProperty] private string _captureShortcut = HotkeyChord.CaptureDefault.ToDisplay();

    public event EventHandler<Guid>? CopyRequested;

    public event EventHandler<Guid>? RemoveRequested;

    public void Replace(IReadOnlyList<HangingItem> items, Guid? flyingId)
    {
        var previous = Cards.ToDictionary(card => card.Id);
        Cards.Clear();
        foreach (var item in items)
        {
            if (!previous.TryGetValue(item.Id, out var card))
            {
                card = new HangingCardViewModel(item, ImageLoader.FromFile(item.Path, 360));
            }

            card.Flying = flyingId == item.Id;
            Cards.Add(card);
        }

        ShowHint = Cards.Count == 0;
        Layout(_width);
    }

    public HangingCardViewModel? Find(Guid id) => Cards.FirstOrDefault(card => card.Id == id);

    public void Layout(double width)
    {
        if (width > 0)
        {
            _width = width;
        }

        var count = Cards.Count;
        for (var index = 0; index < count; index++)
        {
            var card = Cards[index];
            var x = LineLayout.ItemX(index, count, _width, LineLayout.Spacing);
            var photo = LineLayout.PhotoSize(card.PixelWidth, card.PixelHeight);
            card.PhotoWidth = photo.Width;
            card.PhotoHeight = photo.Height;
            card.ChromeWidth = photo.Width + LineLayout.FrameInset * 2;
            card.Left = x - card.ChromeWidth / 2;
            card.Top = LineLayout.RopeY(x, _width) - 6;
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
        Layout(_width);
    }

    public void RequestCopy(Guid id) => CopyRequested?.Invoke(this, id);

    public void RequestRemove(Guid id) => RemoveRequested?.Invoke(this, id);
}
