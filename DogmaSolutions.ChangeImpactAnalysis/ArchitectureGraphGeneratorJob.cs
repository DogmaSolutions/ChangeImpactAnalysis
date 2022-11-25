using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DogmaSolutions.Reflection;
using DogmaSolutions.Tasking;
using DogmaSolutions.Utils;
using DogmaSolutions.Validation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Msagl.Drawing;
using NuGet.Common;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using ILogger = NuGet.Common.ILogger;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ArchitectureGraphGeneratorJob : IDisposable
{
    [CanBeNull] private readonly Func<PackageSpec, bool> _packageSpecFilter;
    [CanBeNull] private readonly Func<IList<TargetFrameworkInformation>, ICollection<TargetFrameworkInformation>> _targetFrameworkFilter;
    [CanBeNull] private readonly Func<IList<LibraryDependency>, IEnumerable<LibraryDependency>> _libraryDependencyFilter;
    [CanBeNull] private readonly Func<IList<PackageDependency>, IEnumerable<PackageDependency>> _packageDependencyFilter;
    [CanBeNull] private readonly IAnalysisContextEventTriggers _eventTriggers;
    [NotNull] private readonly List<string> _edges = new();
    [NotNull] private readonly SemaphoreSlim _globalSemaphore = new(1, 1);
    [NotNull] private readonly SemaphoreSlim _addEdgeSemaphore = new(1, 1);
    [NotNull] private readonly Graph _graph;
    [NotNull] private readonly IServiceProvider _serviceProvider;
    [NotNull] private readonly IArchitecture _architecture;
    [NotNull] private readonly ILogger<ArchitectureGraphGeneratorJob> _logger;
    private bool _isDisposed;

    public ArchitectureGraphGeneratorJob(
        [NotNull] IServiceProvider serviceProvider,
        [NotNull] IArchitecture architecture,
        [CanBeNull] Func<PackageSpec, bool> packageSpecFilter = null,
        [CanBeNull] Func<IList<TargetFrameworkInformation>, ICollection<TargetFrameworkInformation>> targetFrameworkFilter = null,
        [CanBeNull] Func<IList<LibraryDependency>, IEnumerable<LibraryDependency>> libraryDependencyFilter = null,
        [CanBeNull] Func<IList<PackageDependency>, IEnumerable<PackageDependency>> packageDependencyFilter = null,
        [CanBeNull] IAnalysisContextEventTriggers eventTriggers = null
    )
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = _serviceProvider.GetRequiredService<ILogger<ArchitectureGraphGeneratorJob>>();
        _architecture = architecture ?? throw new ArgumentNullException(nameof(architecture));

        _logger.LogInformation("Validating the provided architecture file");
        _architecture.ValidateDataAnnotations();
        foreach (var layer in _architecture.Layers)
        {
            layer.ValidateDataAnnotations();
        }
        _logger.LogInformation("The provided architecture file is OK");

        _packageSpecFilter = packageSpecFilter;
        _targetFrameworkFilter = targetFrameworkFilter;
        _libraryDependencyFilter = libraryDependencyFilter;
        _packageDependencyFilter = packageDependencyFilter;
        _eventTriggers = eventTriggers;
        _graph = new Graph(_architecture.Name);
    }

    protected virtual Task<DependencyGraphSpec> GenerateDependencyGraph([NotNull] string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path cannot be null or whitespace.", nameof(projectPath));

        if (!File.Exists(projectPath))
            throw new ArgumentException($"The project path '{projectPath}' does not exist", nameof(projectPath));

        var dgOutput = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        var arguments = new[] { "msbuild", $"\"{projectPath}\"", "/t:GenerateRestoreGraphFile", $"/p:RestoreGraphOutputPath={dgOutput}" };

        _logger.LogInformation($"Analyzing restore-graph of Solution file '{projectPath}'");

        var dotNetRunner = new DotNetRunner();
        var runStatus = dotNetRunner.Run(Path.GetDirectoryName(projectPath), arguments);

        if (runStatus == 0)
        {
            _logger.LogInformation($"Restore-graph successfully resolved");
            var res = DependencyGraphSpec.Load(dgOutput);
            return Task.FromResult(res);
        }
        else
        {
            _logger.LogError("Unable to process the the project '{Path}'", projectPath);
            throw new InvalidOperationException($"Unable to process the the project '{projectPath}'");
        }
    }


    public async Task<Graph> Process(CancellationToken cancellationToken)
    {
        if (_isDisposed)
            throw new ObjectDisposedException("The job has been disposed");

        var start = DateTime.UtcNow;

        try
        {
            await _globalSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("A new architecture analysis job is starting");

            var dependencyGraph = await GenerateDependencyGraph(_architecture.Layers.Last().SolutionFileLocation).ConfigureAwait(false);

            var targetProjects = dependencyGraph.Projects.Where(
                    p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference &&
                         (_packageSpecFilter == null || _packageSpecFilter(p))).
                ToArray();

            var progressCount = 0;
            var totalProjectsCount = targetProjects.Length;
            await Parallel.ForEachAsync(
                    targetProjects,
                    cancellationToken,
                    async (project, ct) =>
                    {
                        _logger.LogInformation("Started analyzing the project '{Project}'", project.Name);
                        var lockFile = await GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath).ConfigureAwait(false);

                        var frameworks = _targetFrameworkFilter != null ? _targetFrameworkFilter(project.TargetFrameworks) : project.TargetFrameworks;
                        await Parallel.ForEachAsync(
                                frameworks,
                                ct,
                                async (targetFramework, ct2) =>
                                {
                                    var projectNameAndFramework = frameworks.Count == 1 ? project.Name : project.Name + " (" + targetFramework.FrameworkName + ")";

                                    _logger.LogInformation("Processing " + projectNameAndFramework);

                                    var edge0 = await SafeAddEdge(_architecture.Name, projectNameAndFramework, ct2).ConfigureAwait(false);
                                    if (edge0 == null)
                                        return; // we already added this sub-graph

                                    var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));
                                    if (lockFileTargetFramework != null)
                                    {
                                        var filteredDependencies = _libraryDependencyFilter != null
                                            ? _libraryDependencyFilter(targetFramework.Dependencies)
                                            : targetFramework.Dependencies;
                                        await Parallel.ForEachAsync(
                                                filteredDependencies,
                                                ct2,
                                                async (dependency, ct3) =>
                                                {
                                                    var edge = await SafeAddEdge(projectNameAndFramework, dependency.Name, ct3).ConfigureAwait(false);
                                                    if (edge == null)
                                                        return; // we already added this sub-graph

                                                    var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(
                                                        library => string.Equals(library.Name, dependency.Name, StringComparison.InvariantCultureIgnoreCase));

                                                    await AnalyzeDependency(
                                                            dependency.Name,
                                                            projectLibrary,
                                                            dependency,
                                                            lockFileTargetFramework,
                                                            ct3).
                                                        ConfigureAwait(false);
                                                }).
                                            ConfigureAwait(false);
                                    }
                                }).
                            ConfigureAwait(false);

                        _logger.LogInformation("Completed analyzing the project '{Project}'", project.Name);

                        Interlocked.Increment(ref progressCount);

                        var eventTriggers = _eventTriggers;
                        if (eventTriggers != null)
                        {
                            var percentageProgress = ((float)progressCount / (float)totalProjectsCount) * 100.0;
                            eventTriggers.OnProgress(new ValueEventArgs<int>((int)percentageProgress)).RunAndForget();
                        }

                    }).
                ConfigureAwait(false);

            var end = DateTime.UtcNow;
            var elapsed = end - start;
            _logger.LogInformation("Architecture analysis job successfully completed after {Delay}", elapsed);

            return _graph;
        }
        catch (Exception exc)
        {
            var end = DateTime.UtcNow;
            var elapsed = end - start;
            _logger.LogInformation("Architecture analysis job failed after {Delay}. {Exception}", elapsed, exc.GetReadableMessage());
            throw;
        }
        finally
        {
            _globalSemaphore.Release();
        }
    }


    private Task<LockFile> GetLockFile(string projectPath, string outputPath)
    {
        // Run the restore command
        var dotNetRunner = new DotNetRunner();
        var arguments = new[] { "restore", $"\"{projectPath}\"" };
        var runStatus = dotNetRunner.Run(Path.GetDirectoryName(projectPath), arguments);
        if (runStatus != 0)
            throw new InvalidOperationException($"Cannot analyze '{projectPath}'");

        // Load the lock file
        var lockFilePath = Path.Combine(outputPath, "project.assets.json");
        return GetLockFile(lockFilePath, NullLogger.Instance);
    }

    private async Task<LockFile> GetLockFile(string lockFilePath, ILogger logger)
    {
        if (!File.Exists(lockFilePath))
            return null;

        var format = new LockFileFormat();
        // A corrupt lock file will log errors and return null
        var lockFile = await FileUtility.SafeReadAsync(lockFilePath, (stream, path) => Task.FromResult(format.Read(stream, logger, path))).ConfigureAwait(false);
        return lockFile;
    }

    private async Task AnalyzeDependency(
        string parentNode,
        LockFileTargetLibrary projectLibrary,
        object packageDependency,
        LockFileTarget lockFileTargetFramework,
        CancellationToken cancellationToken)
    {
        if (projectLibrary == null)
        {
            _logger.LogWarning($"Dependency '{packageDependency}' cannot be resolved");
            return;
        }

        var packageDependencies = _packageDependencyFilter != null ? _packageDependencyFilter(projectLibrary.Dependencies) : projectLibrary.Dependencies;

        await Parallel.ForEachAsync(
                packageDependencies,
                cancellationToken,
                async (childDependency, ct) =>
                {
                    var edge = await SafeAddEdge(parentNode, childDependency.Id, ct).ConfigureAwait(false);
                    if (edge == null)
                        return; // we already added this sub-graph

                    var childLibrary =
                        lockFileTargetFramework.Libraries.
                            FirstOrDefault(library => string.Equals(library.Name, childDependency.Id, StringComparison.InvariantCultureIgnoreCase));

                    await AnalyzeDependency(
                            childDependency.Id,
                            childLibrary,
                            childDependency,
                            lockFileTargetFramework,
                            ct).
                        ConfigureAwait(false);
                }).
            ConfigureAwait(false);
    }

    private async Task<Edge> SafeAddEdge(string from, string to, CancellationToken cancellationToken)
    {
        if (_isDisposed)
            throw new ObjectDisposedException("The job has been disposed");

        try
        {
            await _addEdgeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            /*
    if (_nodes.Contains(from, StringComparer.InvariantCultureIgnoreCase))
    return null; // already generated a sub-graph for this "from"
       */
            var key = from + "_" + to;
            if (_edges.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                return null; // already generated a sub-graph for this  edge

            _edges.Add(key);
                
            var fromNode = _graph.FindNode(from);
            if (fromNode == null)
            {
                fromNode= new Node(from);
                _graph.AddNode(fromNode);
            }

            var toNode = _graph.FindNode(to);
            if (toNode == null)
            {
                toNode = new Node(to);
                _graph.AddNode(toNode);
            }
          
            var edge = new Edge(fromNode, toNode, ConnectionToGraph.Connected);
            _graph.AddPrecalculatedEdge(edge);

            return edge;
        }
        finally
        {
            _addEdgeSemaphore.Release();
        }
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (_isDisposed)
            return;

        if (isDisposing)
        {
            _globalSemaphore.Dispose();
            _addEdgeSemaphore.Dispose();
        }

        _isDisposed = true;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}