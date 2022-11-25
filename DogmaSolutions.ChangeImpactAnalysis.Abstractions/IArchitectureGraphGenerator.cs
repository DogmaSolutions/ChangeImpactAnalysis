using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace DogmaSolutions.ChangeImpactAnalysis
{
    public interface IArchitectureGraphGenerator
    {
        Task<Graph> GenerateArchitectureGraph(
            IArchitecture architecture,
            Func<PackageSpec, bool> packageSpecFilter = null,
            Func<IList<TargetFrameworkInformation>, ICollection<TargetFrameworkInformation>> targetFrameworkFilter = null,
            Func<IList<LibraryDependency>, IEnumerable<LibraryDependency>> libraryDependencyFilter = null,
            Func<IList<PackageDependency>, IEnumerable<PackageDependency>> packageDependencyFilter = null,
            IAnalysisContextEventTriggers eventTriggers = null,
            CancellationToken cancellationToken = default);
    }
}