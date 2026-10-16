using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// The rounded tab bar that floats above the page, as in the design. Shell's own tab bar is hidden
/// (Styles.xaml); this bar switches Shell tabs by route. It is shown only after authentication.
/// </summary>
public class FloatingTabBar : ContentView
{
    public const string HomeRoute = "MainPage";
    public const string ListRoute = "SpendingListPage";
    public const string SpecialExpenseRoute = "SpecialExpenseListPage";
    public const string SettingsRoute = "SettingPage";

    static readonly (string Route, string Title, string Glyph)[] Tabs =
    [
        (HomeRoute, "ホーム", Icons.Home),
        (ListRoute, "一覧", Icons.List),
        (SpecialExpenseRoute, "特別出費", Icons.Gift),
        (SettingsRoute, "設定", Icons.Settings),
    ];

    public static readonly BindableProperty CurrentProperty =
        BindableProperty.Create(nameof(Current), typeof(string), typeof(FloatingTabBar), HomeRoute, propertyChanged: (b, _, _) => ((FloatingTabBar)b).Build());

    public FloatingTabBar()
    {
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(12, 0, 12, 16);
        SetBinding(IsVisibleProperty, new Binding(nameof(SharedProperty.IsUnlocked), source: SharedProperty.Instance));
        Build();
    }

    /// <summary>The route of the tab this page belongs to.</summary>
    public string Current { get => (string)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value); }

    const double BarHeight = 76;

    void Build()
    {
        var grid = new Grid { ColumnSpacing = 4 };
        for (var i = 0; i < Tabs.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var (route, title, glyph) = Tabs[i];
            var selected = route == Current;
            var item = BuildItem(route, title, glyph, selected);
            Grid.SetColumn(item, i);
            grid.Children.Add(item);
        }

        var bar = new Border
        {
            HeightRequest = BarHeight,
            Padding = 8,
            StrokeShape = new RoundRectangle { CornerRadius = 38 },
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.16f, Radius = 24, Offset = new Point(0, 6) },
            Content = grid,
        };
        bar.SetDynamicResource(BackgroundColorProperty, "Surface");
        Content = bar;
    }

    static View BuildItem(string route, string title, string glyph, bool selected)
    {
        var icon = new IconPath { Glyph = glyph, Size = 22 };
        icon.SetDynamicResource(IconPath.ColorProperty, selected ? "AccentInk" : "InkMuted");

        var label = new Label
        {
            Text = title,
            FontSize = 12,
            // Fixed size so all four labels fit the bar at any text size; the icons and descriptions stay.
            FontAutoScalingEnabled = false,
            FontFamily = selected ? "ZenMaruBold" : "ZenMaru",
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap,
        };
        label.SetDynamicResource(Label.TextColorProperty, selected ? "AccentInk" : "InkMuted");

        var body = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 30 },
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children = { icon, label },
            },
        };
        if (selected)
        {
            body.SetDynamicResource(BackgroundColorProperty, "AccentSoft");
        }

        return new Pressable
        {
            Body = body,
            CornerRadius = 30,
            Description = selected ? $"{title}（表示中）" : title,
            Command = new AsyncRelayCommand(async () =>
            {
                if (!selected) await Shell.Current.GoToAsync("//" + route, false);
            }),
        };
    }
}
