using System;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public class EvidenceVsEvidenceQuery : IEvidenceVsEvidenceQuery
    {
        public string QueryId { get; }
        public ContradictionType ExpectedType { get; }
        public string PrimaryEvidenceId { get; }
        public string SecondaryEvidenceId { get; }

        public EvidenceVsEvidenceQuery(string primaryEvidenceId, string secondaryEvidenceId, ContradictionType expectedType)
        {
            PrimaryEvidenceId = primaryEvidenceId ?? throw new ArgumentNullException(nameof(primaryEvidenceId));
            SecondaryEvidenceId = secondaryEvidenceId ?? throw new ArgumentNullException(nameof(secondaryEvidenceId));
            ExpectedType = expectedType;
            QueryId = $"eve_{primaryEvidenceId}_{secondaryEvidenceId}_{DateTime.UtcNow.Ticks}";
        }
    }
}
