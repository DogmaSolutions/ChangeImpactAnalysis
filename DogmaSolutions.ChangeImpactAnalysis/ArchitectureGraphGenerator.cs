using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Msagl.Drawing;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace DogmaSolutions.ChangeImpactAnalysis
{
    public class ArchitectureGraphGenerator : IArchitectureGraphGenerator
    {
        [NotNull] private readonly IServiceProvider _serviceProvider;

        public ArchitectureGraphGenerator([NotNull] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }


        public async Task<Graph> GenerateArchitectureGraph(
            IArchitecture architecture,
            Func<PackageSpec, bool> packageSpecFilter = null,
            Func<IList<TargetFrameworkInformation>, ICollection<TargetFrameworkInformation>> targetFrameworkFilter = null,
            Func<IList<LibraryDependency>, IEnumerable<LibraryDependency>> libraryDependencyFilter = null,
            Func<IList<PackageDependency>, IEnumerable<PackageDependency>> packageDependencyFilter = null,
            IAnalysisContextEventTriggers eventTriggers = null,
            CancellationToken cancellationToken = default)
        {
            using var job = new ArchitectureGraphGeneratorJob(
                _serviceProvider,
                architecture,
                packageSpecFilter,
                targetFrameworkFilter,
                libraryDependencyFilter,
                packageDependencyFilter,
                eventTriggers);

            return await job.Process(cancellationToken).ConfigureAwait(false);
        }
    }
}