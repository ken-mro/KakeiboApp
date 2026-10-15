using System.Windows.Input;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// Makes composed content (icon + text, a whole row, a card) act as one button.
/// An invisible Button is laid over the content, so screen readers announce it as a button with
/// <see cref="Description"/>, and the content itself is left out of the accessibility tree.
/// </summary>
[ContentProperty(nameof(Body))]
public class Pressable : ContentView
{
    readonly Button _button;
    readonly ContentView _host = new();

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(Pressable), null, propertyChanged: (b, _, n) => ((Pressable)b)._button.Command = (ICommand?)n);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(Pressable), null, propertyChanged: (b, _, n) => ((Pressable)b)._button.CommandParameter = n);

    public static readonly BindableProperty DescriptionProperty =
        BindableProperty.Create(nameof(Description), typeof(string), typeof(Pressable), null, propertyChanged: (b, _, n) => SemanticProperties.SetDescription(((Pressable)b)._button, (string?)n));

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(Pressable), null, propertyChanged: (b, _, n) => SemanticProperties.SetHint(((Pressable)b)._button, (string?)n));

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(int), typeof(Pressable), 0, propertyChanged: (b, _, n) => ((Pressable)b)._button.CornerRadius = (int)n);

    public static readonly BindableProperty BodyProperty =
        BindableProperty.Create(nameof(Body), typeof(View), typeof(Pressable), null, propertyChanged: (b, _, n) => ((Pressable)b)._host.Content = (View?)n);

    public Pressable()
    {
        _button = new Button
        {
            Style = (Style)Application.Current!.Resources["OverlayButton"],
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        _button.Clicked += (_, e) => Clicked?.Invoke(this, e);
        AutomationProperties.SetExcludedWithChildren(_host, true);
        Content = new Grid { Children = { _host, _button } };
    }

    public event EventHandler? Clicked;

    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public int CornerRadius { get => (int)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The visible content. Hidden from screen readers; <see cref="Description"/> is read instead.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }
}
