using System.Windows;

namespace ProtoFact.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply the persisted theme preference (or the Windows system theme)
        // before the main window is constructed, so it renders themed from
        // the first frame.
        ThemeManager.Initialize();
    }
}
