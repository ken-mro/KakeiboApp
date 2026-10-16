using KakeiboApp.ViewModels;
using KakeiboApp.Views.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views;

public partial class SettingPage : ContentPage, ITourHost
{
    public Grid TourRoot => root;

    public void SetTouring(bool touring) { }

    public async Task<View?> PrepareTourTargetAsync(string target)
    {
        if (target != "guide") return null;
        await scroll.ScrollToAsync(guideRow, ScrollToPosition.Center, false);
        return guideRow;
    }

    readonly SettingPageViewModel _vm;

    public SettingPage(SettingPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        BuildThemeChips();
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
