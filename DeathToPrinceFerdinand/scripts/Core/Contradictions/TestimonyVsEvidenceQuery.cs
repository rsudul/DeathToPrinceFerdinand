using System;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public class TestimonyVsEvidenceQuery : ITestimonyVsEvidenceQuery
    {
        public string QueryId { get; }
        public ContradictionType ExpectedType { get; }
        public string TestimonyStatementId { get; }
        public string EvidenceId { get; }

        public TestimonyVsEvidenceQuery(string testimonyStatementId, string evidenceId, ContradictionType expectedType)
        {
            TestimonyStatementId = testimonyStatementId ?? throw new ArgumentNullException(nameof(testimonyStatementId));
            EvidenceId = evidenceId ?? throw new ArgumentNullException(nameof(evidenceId));
            ExpectedType = expectedType;
            QueryId = $"tve_{testimonyStatementId}_{evidenceId}_{DateTime.UtcNow.Ticks}";
        }
    }
}
