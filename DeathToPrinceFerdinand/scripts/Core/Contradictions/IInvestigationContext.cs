using System.Collections.Generic;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IInvestigationContext
    {
        TestimonyStatement? GetTestimony(string statementId);
        Evidence? GetEvidence(string evidenceId);
        DossierState? GetDossier(string suspectId);

        IEnumerable<TestimonyStatement> GetAllTestimony();
        IEnumerable<Evidence> GetAllEvidence();
        IEnumerable<ContradictionResult> GetResolvedContradictions();

        Task UpdateTestimonyAsync(string statementId, string amendedText);
        Task AddEvidenceAsync(Evidence evidence);
        Task AddCrossReferenceAsync(CrossReference reference);
        Task MarkContradictionResolvedAsync(string contradictionId, ContradictionResolution resolution);
    }
}
