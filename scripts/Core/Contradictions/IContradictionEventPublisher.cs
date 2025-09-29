using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;


namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IContradictionEventPublisher
    {
        Task PublishContradictionFoundAsync(ContradictionResult result);
        Task PublishContradictionResolvedAsync(string contradictionId, ContradictionResolution resolution);
        Task PublishDossierUpdatedAsync(string suspectId);
        Task PublishEvidenceUnlockedAsync(string evidenceId);
        Task PublishCrossReferenceCreatedAsync(CrossReference reference);
    }
}
