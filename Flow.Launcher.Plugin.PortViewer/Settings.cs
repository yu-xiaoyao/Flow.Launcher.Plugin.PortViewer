namespace Flow.Launcher.Plugin.PortViewer;

public class Settings : BaseModel
{
    public bool ResolveProcessName { get; set; }

    public Settings()
    {
        ResolveProcessName = true;
    }
}