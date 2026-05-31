using Avalonia;
using Avalonia.Controls;
using DialogHostAvalonia;

namespace Blabbermouth;

public partial class DialogBox : UserControl
{
    public DialogBox(DialogHost host, string message, string title, params string[] buttons)
    {
        InitializeComponent();
        TitleText.Text = title;
        ContentText.Text = message;
        
        foreach (string name in buttons)
        {
            Button button = new()
            {
                Content = name,
                Margin = new(2, 2, 2, 0),
                Padding = new(4, 2),
                Classes = { "DialogButton" },
                CommandParameter = name,
            };

            button.Click += (_, _) => host.CloseDialogCommand.Execute(name);
            ButtonsPanel.Children.Add(button);
        }

        Thickness t = ButtonsPanel.Children[0].Margin;
        ButtonsPanel.Children[0].Margin = new(0, t.Top, t.Right, t.Bottom);
        t = ButtonsPanel.Children[^1].Margin;
        ButtonsPanel.Children[^1].Margin = new(t.Left, t.Top, 0, t.Bottom);
    }
}