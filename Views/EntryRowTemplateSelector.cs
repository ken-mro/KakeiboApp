using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Views;

/// <summary>
/// Rows that can be deleted get the swipe-to-delete template.
/// </summary>
public class EntryRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate Deletable { get; set; } = default!;
    public DataTemplate Plain { get; set; } = default!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is EntryRow { CanDelete: true } ? Deletable : Plain;
}
