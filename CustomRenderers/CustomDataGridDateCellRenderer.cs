using Syncfusion.Maui.DataGrid;

namespace KakeiboApp.CustomRenderers;

public class CustomDataGridDateCellRenderer: DataGridDateCellRenderer
{
    private readonly Color _selectionBackground;
    public CustomDataGridDateCellRenderer(Brush selectionBackground)
    {
        _selectionBackground = ((SolidColorBrush)selectionBackground).Color;
    }

    public override void OnInitializeEditView(DataColumnBase dataColumn, DatePicker view)
    {
        base.OnInitializeEditView(dataColumn, view);
        view.BackgroundColor = _selectionBackground;
    }
}
