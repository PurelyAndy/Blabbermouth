using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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