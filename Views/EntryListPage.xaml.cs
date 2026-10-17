using KakeiboApp.ViewModels.Entries;
using KakeiboApp.Views.Controls;

namespace KakeiboApp.Views;

/// <summary>
/// Opened from the home with the route <see cref="Route"/> and a <c>kind</c> query, or used as a tab
/// through <see cref="SpendingListPage"/> and <see cref="SpecialExpenseListPage"/>.
/// </summary>
public partial class EntryListPage : ContentPage, IQueryAttributable, ITourHost
{
    public Grid TourRoot => root;

    public async Task SetTouringAsync(bool touring)
    {
        _vm.IsTouring = touring;
        await _vm.LoadAsync();
    }

    public bool ShowsSample => _vm.ShowsSample;

    public async Task<View?> PrepareTourTargetAsync(string target)
    {
        // The tour explains the month's list, not search results.
        await _vm.CloseSearchAsync();
        // A tab opened for the first time may still be loading; whether the hint shows depends on the rows.
        await _vm.LoadAsync();
        View? view = target switch
        {
            // The hint above the rows says how to edit and delete; an empty list has no hint, so the add button is used.
            "hint" when _vm.HasRows => hintLabel,
            "add" => addButton,
            "search" => searchButton,
            _ => null,
        };
        if (view is null || !view.IsVisible) return null;
        // The hint is in the list's header; the search button stays at the top of the page.
        if (view == hintLabel) ScrollToTop();
        await Task.Delay(100);
        return view;
    }

    // ScrollTo(0) brings the first row to the top and leaves the header above the screen; the header is the
    // native list's first position.
    void ScrollToTop()
    {
#if ANDROID
        if (list.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recycler)
        {
            recycler.ScrollToPosition(0);
            return;
        }
#endif
        list.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
    }

    public const string Route = "entries";

    readonly EntryListViewModel _vm;

    public EntryListPage(EntryListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(EntryListViewModel.IsSearching)) return;
            // Open the keyboard for a new search; put it away when the search is closed.
            Dispatcher.Dispatch(() =>
            {
                if (_vm.IsSearching) searchEntry.Focus();
                else searchEntry.Unfocus();
            });
        };
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

    /// <summary>The back key closes the search first.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (!_vm.IsSearching) return base.OnBackButtonPressed();
        _ = _vm.CloseSearchAsync();
        return true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Undo.Dismiss();
    }
}

public class SpendingListPage(EntryListViewModel vm) : EntryListPage(vm, EntryKind.Spending, FloatingTabBar.ListRoute);

public class SpecialExpenseListPage(EntryListViewModel vm) : EntryListPage(vm, EntryKind.SpecialExpense, FloatingTabBar.SpecialExpenseRoute);
