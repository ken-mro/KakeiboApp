using System.Windows.Input;

namespace KakeiboApp.Views.Controls;

public partial class EntryRowView : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EntryRowView), null,
            propertyChanged: (b, _, n) => ((EntryRowView)b).pressable.Command = (ICommand?)n);

    public EntryRowView()
    {
        InitializeComponent();
    }

    /// <summary>Runs with the row as parameter when the row is tapped (opens the edit sheet).</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        pressable.CommandParameter = BindingContext;
    }
}
