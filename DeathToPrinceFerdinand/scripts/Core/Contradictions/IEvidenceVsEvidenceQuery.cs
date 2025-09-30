namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IEvidenceVsEvidenceQuery : IContradictionQuery
    {
        string PrimaryEvidenceId { get; }
        string SecondaryEvidenceId { get; }
    }
}
