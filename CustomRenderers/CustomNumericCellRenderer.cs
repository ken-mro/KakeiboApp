using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Inputs;

namespace KakeiboApp.CustomRenderers;

public class CustomNumericCellRenderer : DataGridNumericCellRenderer
{
    private readonly Color _selectionBackground;
    public CustomNumericCellRenderer(Brush selectionBackground)
    {
        _selectionBackground = ((SolidColorBrush)selectionBackground).Color;
    }

    public override void OnInitializeEditView(DataColumnBase dataColumn, SfNumericEntry view)
    {
        base.OnInitializeEditView(dataColumn, view);
        view.BackgroundColor = _selectionBackground;
    }
}
