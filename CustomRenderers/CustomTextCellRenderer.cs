using Syncfusion.Maui.DataGrid;

namespace KakeiboApp.CustomRenderers;

public class CustomTextCellRenderer: DataGridTextBoxCellRenderer
{
    private readonly Color _selectionBackground;
    public CustomTextCellRenderer(Brush selectionBackground)
    {
        _selectionBackground = ((SolidColorBrush)selectionBackground).Color;
    }

    public override void OnInitializeEditView(DataColumnBase dataColumn, SfDataGridEntry view)
    {
        base.OnInitializeEditView(dataColumn, view);
        view.BackgroundColor = _selectionBackground;
    }
}
