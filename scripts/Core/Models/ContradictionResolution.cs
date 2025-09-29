using System.Collections.Generic;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class ContradictionResolution
    {
        public string? AmendedTestimony { get; set; }
        public List<string> NewEvidenceIds { get; set; } = new();
        public List<string> UnlockedSuspectIds { get; set; } = new();
        public List<string> DossierUpdates { get; set; } = new();
        public List<CrossReference> CrossReferences { get; set; } = new();

        public bool HasAnyResolution =>
            !string.IsNullOrEmpty(AmendedTestimony) ||
            NewEvidenceIds.Count > 0 ||
            UnlockedSuspectIds.Count > 0 ||
            DossierUpdates.Count > 0 ||
            CrossReferences.Count > 0;
    }
}
