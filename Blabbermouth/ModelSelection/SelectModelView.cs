using System.Threading.Tasks;
using Avalonia.Controls;
using DialogHostAvalonia;

namespace Blabbermouth.ModelSelection;

public abstract class SelectModelView : UserControl
{
    protected static async Task ShowErrorAsync(string message, string title)
    {
        await DialogHost.Show(
            new DialogBox(MainWindow.I.Dialog, message, title, "OK"),
            MainWindow.I.Dialog);
    }
    
    protected static void Close()
    {
        MainWindow.I.DialogNoClickAway.CloseDialogCommand.Execute(null);
    }
}
