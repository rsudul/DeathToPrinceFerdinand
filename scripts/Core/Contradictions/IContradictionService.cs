using System.Collections.Generic;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;


namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IContradictionService
    {
        Task<ContradictionResult> CheckContradictionAsync(IContradictionQuery query);
        Task<IEnumerable<ContradictionResult>> GetPossibleContradictionsAsync(string suspectId);
        Task<ContradictionResult> ApplyResolutionAsync(ContradictionResult result);
        Task<bool> IsContradictionResolvedAsync(string contradictionId);
        Task<IEnumerable<ContradictionResult>> GetUnresolvedContradictionsAsync();
    }
}
