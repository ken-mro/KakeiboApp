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
            "side" => sideCard,
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
        BuildChips(themes, _vm.Themes, _vm.SelectThemeCommand);
        BuildChips(sides, _vm.Sides, _vm.SelectSideCommand);
        SystemBarInsets.Apply(this, root);
    }

    // テーマ's システム・ライト・ダーク and the like as chips; the selected one is AccentSoft with AccentInk text.
    static void BuildChips(Grid grid, IReadOnlyList<ChoiceOption> options, System.Windows.Input.ICommand command)
    {
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var label = new Label
            {
                Text = option.Label,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                MinimumHeightRequest = 44,
            };
            var chip = new Border { StrokeShape = new RoundRectangle { CornerRadius = 22 }, Content = label, Padding = new Thickness(8, 0) };
            var pressable = new Pressable { Body = chip, CornerRadius = 22, Command = command, CommandParameter = option };

            void Update()
            {
                chip.SetDynamicResource(BackgroundColorProperty, option.IsSelected ? "AccentSoft" : "Paper");
                label.SetDynamicResource(Label.TextColorProperty, option.IsSelected ? "AccentInk" : "Ink");
                label.FontFamily = option.IsSelected ? "ZenMaruBold" : "ZenMaru";
                pressable.Description = option.Description;
            }

            option.PropertyChanged += (_, _) => Update();
            Update();
            grid.Add(pressable, i);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        quickAdd.Close();
    }
}
