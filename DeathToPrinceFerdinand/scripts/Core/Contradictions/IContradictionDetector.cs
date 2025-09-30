using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IContradictionDetector
    {
        ContradictionType HandledType { get; }
        Task<ContradictionResult> DetectAsync(IContradictionQuery query, IInvestigationContext context);
        bool CanHandle(IContradictionQuery query);
    }
}
