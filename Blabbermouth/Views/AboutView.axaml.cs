using Avalonia.Controls;
using DialogHostAvalonia;

namespace Blabbermouth.Views;

public partial class AboutView : UserControl
{
    public DialogHost Host { get; }
    public AboutView(DialogHost host)
    {
        Host = host;
        InitializeComponent();
    }
}