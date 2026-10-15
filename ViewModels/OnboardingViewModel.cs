using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KakeiboApp.ViewModels;

public enum GuideIllustrationKind { Home, Add, EditDelete, SavingAndSpecial, Again }

/// <summary>
/// One slide of the guide.
/// </summary>
public record GuideSlide(GuideIllustrationKind Illustration, string Title, string Body, string IllustrationDescription);

/// <summary>
/// The guide shown on first launch and after an update (使い方). Also opened from 設定 › 使い方.
/// </summary>
public partial class OnboardingViewModel(Func<Task> close) : ObservableObject
{
    public IReadOnlyList<GuideSlide> Slides { get; } =
    [
        new(GuideIllustrationKind.Home,
            "ホームの見方",
            "いちばん上のカードが「残りのお金」です。1日あたりに使える金額もわかります。\n\n下の2本のバーは今月のペースです。「日数」より「支出」のバーが先に進んでいたら、使いすぎのサインです。",
            "残りのお金のカードと、日数と支出の2本のバー"),
        new(GuideIllustrationKind.Add,
            "生活費のつけ方",
            "ホームの「＋ 生活費をつける」をタップします。\n\n「いくら使った？」に金額を入れて、「なにに使った？」で種類を選び、「登録」をタップするだけです。メモや日付は、なくても大丈夫です。",
            "生活費をつけるボタンと、種類のボタン"),
        new(GuideIllustrationKind.EditDelete,
            "なおす・けす・もとに戻す",
            "一覧の行をタップするとシートが開き、金額や内容をなおせます。\n\n行を左にスワイプして「けす」をタップすると、けせます。まちがえてけしたときは、画面の下の「もとに戻す」で戻せます。\n\n前のバージョンの「金額に0を入れてけす」方法はなくなりました。",
            "左にスワイプした行と、もとに戻すボタン"),
        new(GuideIllustrationKind.SavingAndSpecial,
            "切り崩しと特別出費",
            "貯金から今月の生活に回すときは、ホームの「貯金の内訳・切り崩し」で貯金をタップして金額を入れます。切り崩したお金は、残りのお金に入ります。\n\n旅行や冠婚葬祭などの大きな出費は、下の「特別出費」タブの「＋ 特別出費をつける」から入れます。特別出費は別枠なので、残りのお金には入りません。",
            "貯金のアイコンと特別出費のアイコン"),
        new(GuideIllustrationKind.Again,
            "もう一度見るには",
            "この使い方は「設定 › 使い方」から、いつでも見られます。\n\nそれでは、はじめましょう。",
            "ぶたの貯金箱"),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLast))]
    [NotifyPropertyChangedFor(nameof(NextText))]
    [NotifyPropertyChangedFor(nameof(PageText))]
    int _position;

    public bool IsLast => Position >= Slides.Count - 1;
    public string NextText => IsLast ? "はじめる" : "次へ";
    public string PageText => $"{Position + 1} / {Slides.Count}";

    partial void OnPositionChanged(int value)
    {
        if (value >= 0 && value < Slides.Count)
        {
            SemanticScreenReader.Default.Announce($"{PageText}。{Slides[value].Title}");
        }
    }

    bool _closing;

    [RelayCommand]
    async Task NextAsync()
    {
        if (IsLast)
        {
            await CloseAsync();
            return;
        }
        Position++;
    }

    [RelayCommand]
    Task SkipAsync() => CloseAsync();

    async Task CloseAsync()
    {
        if (_closing) return;
        _closing = true;
        await close();
    }
}
