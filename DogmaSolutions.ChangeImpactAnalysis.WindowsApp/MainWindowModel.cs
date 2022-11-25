using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DogmaSolutions.Reflection;
using DogmaSolutions.SlidingWindowLogger;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Msagl.Drawing;

namespace DogmaSolutions.ChangeImpactAnalysis.WindowsApp;

public sealed class MainWindowModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Global cancellation token source.
    /// </summary>
    [CanBeNull] private CancellationTokenSource _cts;

    /// <summary>
    /// The WPF GUI dispatcher.
    /// </summary>
    [NotNull] private readonly Dispatcher _dispatcher;
    [NotNull] private readonly IServiceProvider _serviceProvider;

#pragma warning disable CA2213
    [NotNull] private readonly ISlidingWindowLoggerProvider _loggerProvider;
#pragma warning restore CA2213

    [CanBeNull]
    public IArchitecture Architecture
    {
        get => _architecture;
        private set => SetField(ref _architecture, value);
    }

    [CanBeNull] private IImpactAnalyzer _impactAnalyzer;
    private bool _isWorking;
    [NotNull] public ImpactAnalysisParameters AnalysisParameters { get; } = new ImpactAnalysisParameters();
    private readonly AnalysisContext _analysisContext = new();
    [CanBeNull] private string _currentOperation;
    private int _progressPercentage;
    [CanBeNull] private ObservableCollection<ISlidingWindowLoggerProviderMessage> _logs = new();
    [CanBeNull] private string _fileName;
    private ImpactAnalysisDescriptor _analysisDescriptor;
    [NotNull] private readonly ILogger<MainWindowModel> _logger;
    [CanBeNull] private IArchitecture _architecture;

    public event PropertyChangedEventHandler PropertyChanged;

    public MainWindowModel([NotNull] Dispatcher dispatcher, [NotNull] IServiceProvider serviceProvider)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<MainWindowModel>>();
        _analysisContext.Message += AnalysisContext_Message;
        _analysisContext.Progress += AnalysisContext_Progress;
        _analysisContext.BooleanUserInteraction += AnalysisContext_BooleanUserInteraction;
        _loggerProvider = serviceProvider.GetRequiredService<ISlidingWindowLoggerProvider>();
        _loggerProvider.MessageLogged += LoggerProvider_MessageLogged;
    }


    private void LoggerProvider_MessageLogged(object sender, Utils.ValueEventArgs<ISlidingWindowLoggerProviderMessage> args)
    {
        try
        {
            _dispatcher.Invoke(
                 () =>
                 {
                     Logs.Insert(0, args.Value);

                     if (Logs.Count > 100)
                         Logs.RemoveAt(Logs.Count - 1);
                 });
        }
        catch (Exception exc)
        {
            Console.WriteLine(exc.GetReadableMessage());
        }
    }

    private async Task AnalysisContext_BooleanUserInteraction(object sender, UserQuestionEventArgs<bool> e)
    {
        try
        {
            await _dispatcher.InvokeAsync(
            () =>
            {
                CurrentOperation = e.Message;
                var result = MessageBox.Show(e.Message, "User interaction needed", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                e.Answer = result == MessageBoxResult.Yes;
                return Task.CompletedTask;
            });
        }
        catch (Exception exc)
        {
            _logger.LogError(exc.GetReadableMessage());
        }

    }

    private Task AnalysisContext_Progress(object sender, Utils.ValueEventArgs<int> e)
    {
        try
        {
            ProgressPercentage = e.Value;
            return Task.CompletedTask;
        }
        catch (Exception exc)
        {
            _logger.LogError(exc.GetReadableMessage());
            return Task.CompletedTask;
        }
    }

    private async Task AnalysisContext_Message(object sender, UserMessageEventArgs e)
    {
        try
        {
            CurrentOperation = e.Message;
            var icon = e.LogLevel switch
            {
                LogLevel.Trace => MessageBoxImage.Information,
                LogLevel.Debug => MessageBoxImage.Information,
                LogLevel.Information => MessageBoxImage.Information,
                LogLevel.Warning => MessageBoxImage.Warning,
                LogLevel.Error => MessageBoxImage.Error,
                LogLevel.Critical => MessageBoxImage.Exclamation,
                LogLevel.None => MessageBoxImage.Information,
                _ => MessageBoxImage.Information
            };

            await _dispatcher.InvokeAsync(
                () =>
                {
                    MessageBox.Show(e.Message, e.LogLevel.ToString(), MessageBoxButton.OK, icon);
                    return Task.CompletedTask;
                });
        }
        catch (Exception exc)
        {
            _logger.LogError(exc.GetReadableMessage());
        }
    }


    public ImpactAnalysisDescriptor AnalysisDescriptor
    {
        get => _analysisDescriptor;
        set => SetField(ref _analysisDescriptor, value);
    }

    public string CurrentOperation
    {
        get => _currentOperation;
        set => SetField(ref _currentOperation, value);
    }

    public ObservableCollection<ISlidingWindowLoggerProviderMessage> Logs
    {
        get => _logs;
        set => SetField(ref _logs, value);
    }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => SetField(ref _progressPercentage, value);
    }

    public string FileName
    {
        get => _fileName;
        set => SetField(ref _fileName, value);
    }


    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        try
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        catch (Exception exc)
        {
            _logger.LogError(exc.GetReadableMessage());
        }
    }

    public void LoadArchitectureFile(string filePath)
    {
        if (IsWorking || _cts != null)
            throw new InvalidOperationException("An operation is working");

        var fi = new FileInfo(filePath);
        if (!fi.Exists)
            throw new InvalidOperationException($"The file '{filePath}' doesn't exist");

        _logger.LogInformation("Loading architecture from file '{FilePath}'", fi.Name);
        FileName = null;
        var architecture = DogmaSolutions.Json.JsonUtils.DeserializeFromJsonFile<ArchitectureFile>(filePath);
        FileName = fi.Name;

        _impactAnalyzer = string.IsNullOrWhiteSpace(architecture.AnalyzerType)
                    ? new ImpactAnalyzer(_serviceProvider)
                    : (IImpactAnalyzer)Activator.CreateInstance(Type.GetType(architecture.AnalyzerType, true), new object[] { _serviceProvider });

        Architecture = architecture;
        AnalysisParameters.ArchitectureGraphFileName = fi.Name;
        AnalysisParameters.ArtifactsBaseFolderPath = fi.Directory?.FullName;
    }

    public bool IsWorking
    {
        get => _isWorking || _cts != null;
        private set
        {
            if (value == _isWorking) return;
            _isWorking = value;
            OnPropertyChanged();
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public async Task Start()
    {
        if (IsWorking || _cts != null)
            throw new InvalidOperationException("An operation is working");

        var aa = _impactAnalyzer;
        if (aa == null)
            throw new InvalidOperationException("Load an architecture file before starting the analysis");

        var a = Architecture;
        if (a == null)
            throw new InvalidOperationException("Load an architecture file before starting the analysis");

        _cts = new CancellationTokenSource();

        Logs.Clear();

        try
        {
            IsWorking = true;
            AnalysisDescriptor = await aa.Analyze(a, AnalysisParameters, _analysisContext).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                _cts?.Dispose();
                _cts = null;
            }
            finally
            {
                IsWorking = false;
            }
        }
    }


    public Task Stop()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        finally
        {

            IsWorking = false;
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void SaveToSvg(string filePath)
    {
        var graph = AnalysisDescriptor?.ArchitectureGraph;
        if (graph == null)
            throw new InvalidOperationException("The architecture graph is not available");

        var renderer = new Microsoft.Msagl.GraphViewerGdi.GraphRenderer(graph);
        renderer.CalculateLayout();
        Microsoft.Msagl.Drawing.SvgGraphWriter.Write(graph, filePath, null, null, 4);
    }
}