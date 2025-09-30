using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public class ContradictionQueryFactory : IContradictionQueryFactory
    {
        public ITestimonyVsEvidenceQuery CreateTestimonyVsEvidence(string testimonyId, string evidenceId, ContradictionType expectedType)
        {
            return new TestimonyVsEvidenceQuery(testimonyId, evidenceId, expectedType);
        }

        public IEvidenceVsEvidenceQuery CreateEvidenceVsEvidence(string primaryEvidenceId, string secondaryEvidenceId, ContradictionType expectedType)
        {
            return new EvidenceVsEvidenceQuery(primaryEvidenceId, secondaryEvidenceId, expectedType);
        }
    }
}
