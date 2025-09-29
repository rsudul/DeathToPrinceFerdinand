using System.Collections.Generic;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IInvestigationRepository
    {
        Task<Evidence?> GetEvidenceAsync(string id);
        Task<IEnumerable<Evidence>> GetAllEvidenceAsync();
        Task<TestimonyStatement?> GetTestimonyAsync(string id);
        Task<IEnumerable<TestimonyStatement>> GetAllTestimonyAsync();
        Task<DossierState?> GetDossierAsync(string suspectId);
        Task<IEnumerable<DossierState>> GetAllDossiersAsync();

        Task SaveEvidenceAsync(Evidence evidence);
        Task SaveTestimonyAsync(TestimonyStatement testimony);
        Task SaveDossierAsync(DossierState dossier);
        Task SaveContradictionAsync(ContradictionResult contradiction);
    }
}
