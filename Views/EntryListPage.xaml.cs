using KakeiboApp.ViewModels.Entries;
using KakeiboApp.Views.Controls;

namespace KakeiboApp.Views;

/// <summary>
/// Opened from the home with the route <see cref="Route"/> and a <c>kind</c> query, or used as a tab
/// through <see cref="SpendingListPage"/> and <see cref="SpecialExpenseListPage"/>.
/// </summary>
public partial class EntryListPage : ContentPage, IQueryAttributable
{
    public const string Route = "entries";

    readonly EntryListViewModel _vm;

    public EntryListPage(EntryListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected EntryListPage(EntryListViewModel vm, EntryKind kind, string tabRoute) : this(vm)
    {
        vm.Initialize(kind, isTab: true);
        tabBar.Current = tabRoute;
        // Above the floating tab bar.
        addButton.Margin = new Thickness(0, 0, 20, 112);
        undoToast.Margin = new Thickness(16, 0, 16, 108);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("kind", out var kind) && kind is EntryKind entryKind)
        {
            _vm.Initialize(entryKind, isTab: false);
            if (tabBar.Parent is Grid grid) grid.Remove(tabBar);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Undo.Dismiss();
    }
}

public class SpendingListPage(EntryListViewModel vm) : EntryListPage(vm, EntryKind.Spending, FloatingTabBar.ListRoute);

public class SpecialExpenseListPage(EntryListViewModel vm) : EntryListPage(vm, EntryKind.SpecialExpense, FloatingTabBar.SpecialExpenseRoute);
