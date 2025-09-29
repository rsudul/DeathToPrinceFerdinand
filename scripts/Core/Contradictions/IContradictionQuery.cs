using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IContradictionQuery
    {
        string QueryId { get; }
        ContradictionType ExpectedType { get; }
    }
}
