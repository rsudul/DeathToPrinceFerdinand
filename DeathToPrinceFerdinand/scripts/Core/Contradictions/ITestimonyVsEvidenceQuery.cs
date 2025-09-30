namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface ITestimonyVsEvidenceQuery : IContradictionQuery
    {
        string TestimonyStatementId { get; }
        string EvidenceId { get; }
    }
}
