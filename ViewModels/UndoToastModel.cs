using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KakeiboApp.ViewModels;

/// <summary>
/// The 「〜をけしました ／ もとに戻す」 bar shown for a few seconds after a delete.
/// </summary>
public partial class UndoToastModel : ObservableObject
{
    public static readonly TimeSpan VisibleFor = TimeSpan.FromSeconds(8);

    Func<Task>? _undo;
    CancellationTokenSource? _hide;

    [ObservableProperty]
    string _message = string.Empty;

    [ObservableProperty]
    bool _isVisible;

    public void Show(string message, Func<Task> undo)
    {
        _hide?.Cancel();
        _undo = undo;
        Message = message;
        IsVisible = true;
        SemanticScreenReader.Default.Announce($"{message}。もとに戻すボタンがあります");

        var hide = _hide = new CancellationTokenSource();
        _ = HideLaterAsync(hide.Token);
    }

    async Task HideLaterAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(VisibleFor, token);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (token.IsCancellationRequested) return;
                IsVisible = false;
                _undo = null;
            });
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void Dismiss()
    {
        _hide?.Cancel();
        IsVisible = false;
        _undo = null;
    }

    [RelayCommand]
    async Task UndoAsync()
    {
        var undo = _undo;
        Dismiss();
        if (undo is null) return;

        await undo();
        SemanticScreenReader.Default.Announce("もとに戻しました");
    }
}
