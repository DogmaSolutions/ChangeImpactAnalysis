using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DogmaSolutions.Contracts;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ArchitectureLayer : IHasName, IHasDescription
{
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string IdentifiersPrefix { get; set; }

    [Required]
    [MinLength(1,ErrorMessage = "At least one GIT repository location is required")]
    public List<string> GitRepositoryLocations { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string SolutionFileLocation { get; set; }

    public ArchitectureLayerVisualAttributes VisualAttributes { get; set; } = new();
}