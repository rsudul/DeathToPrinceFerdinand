using System;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public class ContradictionResolvedEventArgs
    {
        public string ContradictionId { get; set; } = string.Empty;
        public ContradictionResolution Resolution { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
