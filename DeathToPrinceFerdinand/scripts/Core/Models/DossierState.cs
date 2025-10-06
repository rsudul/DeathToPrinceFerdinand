using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;

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

        public List<string> TestimonyIds { get; set; } = new();
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

        public IEnumerable<TestimonyStatement> GetTestimony(IInvestigationContext context)
        {
            return TestimonyIds
                .Select(id => context.GetTestimony(id))
                .Where(t => t != null);
        }
    }
}
