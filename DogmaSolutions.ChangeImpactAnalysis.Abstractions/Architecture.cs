using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DogmaSolutions.Contracts;

namespace DogmaSolutions.ChangeImpactAnalysis
{
    public class Architecture : IArchitecture, IHasName, IHasDescription
    {
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one architecture layer is required")]
        public IEnumerable<ArchitectureLayer> Layers { get; set; }
    }
}