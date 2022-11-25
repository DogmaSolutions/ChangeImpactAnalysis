using System.Windows.Input;

namespace DogmaSolutions.ChangeImpactAnalysis.WindowsApp;

public static class MainWindowCommands
{
#pragma warning disable SA1401
#pragma warning disable CA2211
    public static RoutedCommand ExportToSvgRoutedCommand = new RoutedCommand();
#pragma warning restore SA1401
#pragma warning restore CA2211
}