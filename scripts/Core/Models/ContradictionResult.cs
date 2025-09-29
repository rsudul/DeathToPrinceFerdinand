using System;
using System.Collections.Generic;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class ContradictionResult
    {
        public bool IsContradiction { get; set; }
        public ContradictionType Type { get; set; }
        public string ContradictionId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ContradictionResolution Resolution { get; set; } = new();

        public List<string> AffectedSuspects { get; set; } = new();
        public List<string> RelatedEvidence { get; set; } = new();

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }
}
