using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Input;
using DogmaSolutions.Reflection;
using JetBrains.Annotations;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace DogmaSolutions.ChangeImpactAnalysis.WindowsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        [NotNull] private readonly IServiceProvider _serviceProvider;
        [NotNull] private readonly ILogger<MainWindow> _logger;
        [NotNull] private readonly MainWindowModel _model;
 

        public MainWindow([NotNull] IServiceProvider serviceProvider, [NotNull] ILogger<MainWindow> logger) : base()
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _model =  new(Dispatcher, serviceProvider);
            DataContext = _model;
            InitializeComponent();
        }

        private void OpenFromFileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !_model.IsWorking;
        }

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenFromFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Opening file...");
                var d = new OpenFileDialog();
                if (d.ShowDialog() == true)
                {
                    _logger.LogInformation("Selected file '{Path}'", d.FileName);
                    _model.LoadArchitectureFile(d.FileName);
                }
            }
            catch (TaskCanceledException exc)
            {
                _logger.LogInformation("Open file canceled");
                Console.WriteLine(exc);
            }
            catch (Exception exc)
            {
                _logger.LogError("Open file error. {Error}",exc.GetReadableMessage());
                MessageBox.Show(this, exc.Message);
            }
        }


        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StopCommand_Executed(sender, e);
            Close();
        }

        private void PlayCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !_model.IsWorking;
        }

        private void StopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _model.IsWorking;
        }

        private void ExportToSvgRouted_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Saving to SVG file...");
                var d = new SaveFileDialog();
                d.AddExtension = true;
                d.CheckPathExists = true;
                d.DefaultExt = "svg";
                if (d.ShowDialog() == true)
                {
                    _logger.LogInformation("Selected file '{Path}'", d.FileName);
                    _model.SaveToSvg(d.FileName);
                }
            }
            catch (TaskCanceledException exc)
            {
                _logger.LogInformation("Save to SVG file canceled");
                Console.WriteLine(exc);
            }
            catch (Exception exc)
            {
                _logger.LogError("Save to SVG file error. {Error}", exc.GetReadableMessage());
                MessageBox.Show(this, exc.Message);
            }
        }

        private void ExportToSvgRouted_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _model.AnalysisDescriptor?.ArchitectureGraph != null;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void PlayCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Starting...");
                await _model.Start().ConfigureAwait(false);
            }
            catch (TaskCanceledException exc)
            {
                _logger.LogInformation("Starting canceled");
                Console.WriteLine(exc);
            }
            catch (Exception exc)
            {
                _logger.LogError("Open file error. {Error}", exc.GetReadableMessage());
                MessageBox.Show(this, exc.Message);
            }
        }

        private async void StopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Stopping...");
                await _model.Stop().ConfigureAwait(false);
            }
            catch (TaskCanceledException exc)
            {
                _logger.LogInformation("Stopping canceled");
                Console.WriteLine(exc);
            }
            catch (Exception exc)
            {
                MessageBox.Show(this, exc.Message);
            }
        }
#pragma warning restore VSTHRD100 // Avoid async void methods
     
    }
}
