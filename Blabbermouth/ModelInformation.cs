namespace Blabbermouth;

public class ModelInformation
{
    public string Eula = null!;
    public string Path = null!;
    public string Version = null!;
    
    public ModelInformation() { }
    public ModelInformation(string eula, string path, string version)
    {
        Eula = eula;
        Path = path;
        Version = version;
    }
}