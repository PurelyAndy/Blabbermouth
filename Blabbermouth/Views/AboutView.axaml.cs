using System.Reflection;
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
        string? version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        version = string.IsNullOrEmpty(version) ? "Unknown" : version.Split('+')[0];
        VersionText.Text = $"Version {version}";
    }
}