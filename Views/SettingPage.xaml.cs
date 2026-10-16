using KakeiboApp.ViewModels;
using KakeiboApp.Views.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views;

public partial class SettingPage : ContentPage, ITourHost
{
    public Grid TourRoot => root;

    // Room below the last card, so the tour can scroll it to the top, clear of its bubble.
    public void SetTouring(bool touring) => bottomSpace.HeightRequest = touring ? 720 : 120;

    public async Task<View?> PrepareTourTargetAsync(string target)
    {
        View? view = target switch
        {
            "guide" => guideCard,
            "carry" => carryRow,
            _ => null,
        };
        if (view is null) return null;
        await scroll.ScrollToAsync(view, ScrollToPosition.Start, false);
        return view;
    }

    readonly SettingPageViewModel _vm;

    public SettingPage(SettingPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        BuildThemeChips();
        BuildTopicRows();
    }

    // くわしい使い方: a row for each topic, styled like the 使い方 row above it.
    void BuildTopicRows()
    {
        foreach (var topic in _vm.GuideTopics)
        {
            var text = new VerticalStackLayout
            {
                Padding = new Thickness(0, 8),
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = topic.Name },
                    new Label { Text = $"{topic.Caption}（{topic.Steps.Count}ページ）", Style = (Style)Application.Current!.Resources["Caption"] },
                },
            };
            var chevron = new IconPath { Glyph = Services.Icons.ChevronRight, Weight = 2.4, Size = 14 };
            chevron.SetDynamicResource(IconPath.ColorProperty, "Ink");
            var grid = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12, MinimumHeightRequest = 56 };
            grid.Add(text, 0);
            grid.Add(chevron, 1);
            topicRows.Add(new Pressable
            {
                Body = grid,
                CornerRadius = 20,
                Command = _vm.OpenTopicCommand,
                CommandParameter = topic,
                Description = $"くわしい使い方、{topic.Name}",
                Hint = "ダブルタップでひらく",
            });
        }
    }

    // システム・ライト・ダーク as three chips; the selected one is AccentSoft with AccentInk text.
    void BuildThemeChips()
    {
        for (var i = 0; i < _vm.Themes.Count; i++)
        {
            var option = _vm.Themes[i];
            var label = new Label
            {
                Text = option.Label,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                MinimumHeightRequest = 44,
            };
            var chip = new Border { StrokeShape = new RoundRectangle { CornerRadius = 22 }, Content = label, Padding = new Thickness(8, 0) };
            var pressable = new Pressable { Body = chip, CornerRadius = 22, Command = _vm.SelectThemeCommand, CommandParameter = option };

            void Update()
            {
                chip.SetDynamicResource(BackgroundColorProperty, option.IsSelected ? "AccentSoft" : "Paper");
                label.SetDynamicResource(Label.TextColorProperty, option.IsSelected ? "AccentInk" : "Ink");
                label.FontFamily = option.IsSelected ? "ZenMaruBold" : "ZenMaru";
                pressable.Description = option.Description;
            }

            option.PropertyChanged += (_, _) => Update();
            Update();
            themes.Add(pressable, i);
        }
    }
}
