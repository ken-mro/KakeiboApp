using KakeiboApp.Services;
using KakeiboApp.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views.Controls;

/// <summary>
/// Small mock-ups of the real screens for the guide, built from the same tokens and controls.
/// Decorative: the slide's text and description are read instead.
/// </summary>
public class GuideIllustration : ContentView
{
    public static readonly BindableProperty KindProperty =
        BindableProperty.Create(nameof(Kind), typeof(GuideIllustrationKind), typeof(GuideIllustration), GuideIllustrationKind.Home,
            propertyChanged: (b, _, _) => ((GuideIllustration)b).Build());

    public GuideIllustration()
    {
        AutomationProperties.SetExcludedWithChildren(this, true);
        InputTransparent = true;
        Build();
    }

    public GuideIllustrationKind Kind { get => (GuideIllustrationKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

    void Build()
    {
        View content = Kind switch
        {
            GuideIllustrationKind.Home => HomeCard(),
            GuideIllustrationKind.Add => AddButtonAndCategories(),
            GuideIllustrationKind.EditDelete => SwipedRow(),
            GuideIllustrationKind.SavingAndSpecial => SavingAndSpecial(),
            _ => Again(),
        };

        var frame = new Border
        {
            Padding = 20,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            MinimumHeightRequest = 220,
            Content = content,
        };
        frame.SetDynamicResource(BackgroundColorProperty, "Hero");
        Content = frame;
    }

    static Label Text(string text, double size = 14, string font = "ZenMaru", string color = "Ink")
    {
        var label = new Label { Text = text, FontSize = size, FontFamily = font, VerticalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Label.TextColorProperty, color);
        return label;
    }

    static Border Surface(View content, double radius = 20, Thickness? padding = null)
    {
        var border = new Border { StrokeShape = new RoundRectangle { CornerRadius = radius }, Padding = padding ?? new Thickness(16, 12), Content = content };
        border.SetDynamicResource(BackgroundColorProperty, "Surface");
        return border;
    }

    static Image Pig(double size) => new() { Source = "pig.png", WidthRequest = size, HeightRequest = size, VerticalOptions = LayoutOptions.Start };

    static View Bar(string label, double ratio, string fill)
    {
        var meter = new Meter { BarHeight = 12, Ratio = ratio, VerticalOptions = LayoutOptions.Center };
        meter.SetDynamicResource(Meter.FillColorProperty, fill);
        var grid = new Grid { ColumnDefinitions = [new(36), new(GridLength.Star)], ColumnSpacing = 8 };
        grid.Add(Text(label, 12, color: "InkMuted"), 0);
        grid.Add(meter, 1);
        return grid;
    }

    static View HomeCard()
    {
        var top = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        top.Add(new VerticalStackLayout
        {
            Spacing = 4,
            Children = { Text("残りのお金", 14, "ZenMaruMedium", "InkMuted"), Text("¥90,000", 34, "ZenMaruBold"), Text("1日あたり ¥5,294", 14, color: "InkMuted") },
        }, 0);
        top.Add(Pig(64), 1);

        return new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                top,
                Surface(new VerticalStackLayout { Spacing = 10, Children = { Bar("日数", 0.48, "Accent"), Bar("支出", 0.70, "Deficit") } }),
            },
        };
    }

    static View CategoryTile(string name, bool selected)
    {
        var style = CategoryStyle.For(name);
        var tile = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            StrokeThickness = 2,
            Padding = new Thickness(4, 10),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new IconChip { Diameter = 36, Glyph = style.Glyph, BackgroundColor = style.Chip, IconColor = style.Icon, HorizontalOptions = LayoutOptions.Center },
                    Text(name, 13, selected ? "ZenMaruBold" : "ZenMaru"),
                },
            },
        };
        tile.SetDynamicResource(BackgroundColorProperty, selected ? "AccentSoft" : "Surface");
        tile.SetDynamicResource(Border.StrokeProperty, selected ? "Accent" : "Surface");
        return tile;
    }

    static View AddButtonAndCategories()
    {
        var plus = new IconPath { Glyph = Icons.Plus, Size = 20, Weight = 2.6 };
        plus.SetDynamicResource(IconPath.ColorProperty, "OnAccent");
        var button = new Border
        {
            HeightRequest = 56,
            Padding = new Thickness(22, 0),
            HorizontalOptions = LayoutOptions.End,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Shadow = new Shadow { Brush = new SolidColorBrush(ThemeTokens.Get("ShadowLavender")), Opacity = 0.3f, Radius = 20, Offset = new Point(0, 8) },
            Content = new HorizontalStackLayout { Spacing = 8, Children = { plus, Text("生活費をつける", 16, "ZenMaruBold", "OnAccent") } },
        };
        button.SetDynamicResource(BackgroundColorProperty, "Accent");

        var tiles = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 10 };
        tiles.Add(CategoryTile("食費", true), 0);
        tiles.Add(CategoryTile("日用品・雑貨", false), 1);
        tiles.Add(CategoryTile("娯楽", false), 2);

        return new VerticalStackLayout { Spacing = 20, Children = { button, tiles } };
    }

    static View SwipedRow()
    {
        var food = CategoryStyle.For("食費");
        var row = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12, Padding = new Thickness(18, 0), HeightRequest = 68 };
        row.Add(new IconChip { Diameter = 38, IconSize = 18, Glyph = food.Glyph, BackgroundColor = food.Chip, IconColor = food.Icon }, 0);
        row.Add(new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { Text("ランチ", 16), Text("食費", 12, color: "InkMuted") } }, 1);
        row.Add(Text("¥1,200", 16, "ZenMaruMedium"), 2);
        var rowCard = new Border { StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0, 24, 0, 24) }, Content = row, Margin = new Thickness(0, 0, 92, 0) };
        rowCard.SetDynamicResource(BackgroundColorProperty, "Surface");

        var trash = new IconPath { Glyph = Icons.Trash, Size = 20, Weight = 2.2 };
        trash.SetDynamicResource(IconPath.ColorProperty, "OnDelete");
        var delete = new VerticalStackLayout
        {
            WidthRequest = 92,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2,
            Children = { trash, Text("けす", 14, "ZenMaruBold", "OnDelete") },
        };
        ((Label)delete.Children[1]).HorizontalTextAlignment = TextAlignment.Center;

        var swipe = new Border { StrokeShape = new RoundRectangle { CornerRadius = 24 }, HeightRequest = 68, Content = new Grid { Children = { delete, rowCard } } };
        swipe.SetDynamicResource(BackgroundColorProperty, "DeleteSurface");

        var undo = new Border { StrokeShape = new RoundRectangle { CornerRadius = 22 }, Padding = new Thickness(16, 0), HeightRequest = 44, Content = Text("もとに戻す", 14, "ZenMaruBold", "OnToastButton") };
        undo.SetDynamicResource(BackgroundColorProperty, "ToastButton");
        var toastGrid = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        toastGrid.Add(Text("「ランチ」をけしました", 14, color: "OnToast"), 0);
        toastGrid.Add(undo, 1);
        var toast = new Border { StrokeShape = new RoundRectangle { CornerRadius = 28 }, Padding = new Thickness(20, 6, 8, 6), Content = toastGrid };
        toast.SetDynamicResource(BackgroundColorProperty, "Toast");

        return new VerticalStackLayout { Spacing = 20, Children = { swipe, toast } };
    }

    static View Explained(CategoryStyle style, string title, string note)
    {
        var grid = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)], ColumnSpacing = 12 };
        grid.Add(new IconChip { Diameter = 44, Glyph = style.Glyph, BackgroundColor = style.Chip, IconColor = style.Icon }, 0);
        grid.Add(new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { Text(title, 16, "ZenMaruBold"), Text(note, 12, color: "InkMuted") } }, 1);
        return Surface(grid, 22);
    }

    static View SavingAndSpecial() => new VerticalStackLayout
    {
        Spacing = 12,
        Children =
        {
            Explained(CategoryStyle.SavingBalance, "切り崩し", "残りのお金に入ります"),
            Explained(CategoryStyle.SpecialExpense, "特別出費", "別枠です。残りのお金には入りません"),
        },
    };

    static View Again()
    {
        var chip = new IconChip { Diameter = 40, Glyph = Icons.Help };
        chip.SetDynamicResource(BackgroundColorProperty, "AccentSoft");
        chip.SetDynamicResource(IconChip.IconColorProperty, "AccentInk");
        var chevron = new IconPath { Glyph = Icons.ChevronRight, Size = 14, Weight = 2.4 };
        chevron.SetDynamicResource(IconPath.ColorProperty, "Ink");
        var row = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        row.Add(chip, 0);
        row.Add(Text("使い方", 16, "ZenMaruBold"), 1);
        row.Add(chevron, 2);

        return new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                new Image { Source = "pig.png", WidthRequest = 96, HeightRequest = 96, HorizontalOptions = LayoutOptions.Center },
                Text("設定", 14, "ZenMaruMedium", "InkMuted"),
                Surface(row, 22),
            },
        };
    }
}
