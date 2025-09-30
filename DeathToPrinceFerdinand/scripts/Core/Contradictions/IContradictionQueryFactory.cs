using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IContradictionQueryFactory
    {
        ITestimonyVsEvidenceQuery CreateTestimonyVsEvidence(string testimonyId, string evidenceId, ContradictionType expectedType);
        IEvidenceVsEvidenceQuery CreateEvidenceVsEvidence(string primaryEvidenceId, string secondaryEvidenceId, ContradictionType expectedType);
    }
}
