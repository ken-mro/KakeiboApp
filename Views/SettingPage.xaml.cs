using KakeiboApp.ViewModels;
using KakeiboApp.Views.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace KakeiboApp.Views;

public partial class SettingPage : ContentPage, ITourHost
{
    public Grid TourRoot => root;

    // The cards stay where they are seen without the tour: no extra room to scroll the last one to the top.
    public Task SetTouringAsync(bool touring)
    {
        if (!touring) quickAdd.Opacity = 1;
        return Task.CompletedTask;
    }

    public async Task<View?> PrepareTourTargetAsync(string target)
    {
        View? view = target switch
        {
            "guide" => guideCard,
            "carry" => carryRow,
            _ => null,
        };
        if (view is null) return null;
        // The floating add button would lie over the card outlined below it, as on the home.
        quickAdd.Opacity = 0;
        await TourGeometry.ScrollNearTopAsync(scroll, view);
        return view;
    }

    readonly SettingPageViewModel _vm;

    public SettingPage(SettingPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        BuildThemeChips();
        SystemBarInsets.Apply(this, root);
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        quickAdd.Close();
    }
}
