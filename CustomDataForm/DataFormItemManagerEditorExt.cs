using Syncfusion.Maui.DataForm;
using Syncfusion.Maui.Inputs;

namespace KakeiboApp.CustomDataForm;

public class DataFormItemManagerEditorExt : DataFormItemManager
{
    public override void InitializeDataEditor(DataFormItem dataFormItem, View editor)
    {
        if (editor is SfNumericEntry numEntry)
        {
            numEntry.ShowClearButton = false;
        }

        if (editor is SfComboBox sfComboBox)
        {
            sfComboBox.IsClearButtonVisible = false;
        }
    }
}
