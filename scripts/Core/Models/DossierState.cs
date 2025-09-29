using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class DossierState
    {
        [Required]
        public string SuspectId { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Alias { get; set; }
        public string? Codename { get; set; }

        public List<TestimonyStatement> Testimony { get; set; } = new();
        public List<string> LinkedEvidenceIds { get; set; } = new();
        public List<ContradictionResult> Contradictions { get; set; } = new();
        public List<CrossReference> Relationships { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public string DisplayName =>
            !string.IsNullOrEmpty(Alias) ? $"{Name} (alias: {Alias})" : Name;

        public string FullDisplayName =>
            !string.IsNullOrEmpty(Codename) ? $"{DisplayName} - {Codename}" : DisplayName;

        public int ResolvedContradictionsCount => Contradictions.Count(c => c.Resolution.HasAnyResolution);

        public bool HasUnresolvedContradictions => Contradictions.Any(c => !c.Resolution.HasAnyResolution);
    }
}
