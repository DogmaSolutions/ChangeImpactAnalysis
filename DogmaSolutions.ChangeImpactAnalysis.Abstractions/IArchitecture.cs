using System.Collections.Generic;
using DogmaSolutions.Contracts;

namespace DogmaSolutions.ChangeImpactAnalysis;

public interface IArchitecture : IHasName_Get, IHasDescription_Get
{
    IEnumerable<ArchitectureLayer> Layers { get; }
}