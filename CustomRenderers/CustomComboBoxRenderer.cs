using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Inputs;

namespace KakeiboApp.CustomRenderers;

public class CustomComboBoxRenderer : DataGridComboBoxRenderer
{
    private readonly Color _selectionBackground;
    public CustomComboBoxRenderer(Brush selectionBackground)
    {
        _selectionBackground = ((SolidColorBrush)selectionBackground).Color;
    }

    public override void OnInitializeEditView(DataColumnBase dataColumn, SfComboBox view)
    {
        base.OnInitializeEditView(dataColumn, view);
        view.BackgroundColor = _selectionBackground;
        view.DropDownIconColor = view.TextColor;
        view.IsClearButtonVisible = false;
    }
}
