using System.Text.RegularExpressions;
using KakeiboApp.Views.Controls;

namespace KakeiboApp.Views;

/// <summary>ライセンス, opened from 設定 with the route <see cref="Route"/>.</summary>
public partial class LicensesPage : ContentPage
{
    public const string Route = "licenses";

    public LicensesPage()
    {
        InitializeComponent();
        SystemBarInsets.Apply(this, root);
        back.Clicked += async (_, _) => await Shell.Current.GoToAsync("..");
        Loaded += async (_, _) => await ShowAsync();
    }

    /// <summary>One label per paragraph: the whole text in one label was slow to lay out.</summary>
    async Task ShowAsync()
    {
        if (body.Children.Count > 0) return;
        string text;
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("ThirdPartyNotices.txt");
            using var reader = new StreamReader(stream);
            text = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return;
        }
        foreach (var paragraph in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var heading = paragraph.StartsWith('■');
            var label = new Label
            {
                // The license texts break their lines for a fixed width; a line going on with the sentence joins the one
                // before, so the text wraps to the screen instead.
                Text = heading ? paragraph : ContinuedLine().Replace(paragraph, " "),
                FontSize = heading ? 15 : 12,
                LineBreakMode = LineBreakMode.WordWrap,
            };
            label.SetDynamicResource(Label.TextColorProperty, heading ? "Ink" : "InkMuted");
            body.Add(label);
        }
    }

    [GeneratedRegex(@"[ \t]*\n[ \t]*(?=[a-z(""])")]
    private static partial Regex ContinuedLine();
}
