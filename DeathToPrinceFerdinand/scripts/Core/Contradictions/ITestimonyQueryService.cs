using DeathToPrinceFerdinand.scripts.Core.Models;
using System.Collections.Generic;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface ITestimonyQueryService
    {
        IEnumerable<TestimonyStatement> FindByTopic(string topic);
        IEnumerable<TestimonyStatement> FindBySuspect(string suspectId);
        IEnumerable<TestimonyStatement> FindByKeywords(params string[] keywords);
        bool HasSuspectMentioned(string suspectId, string topic);
        bool HasSuspectClaimedRelationship(string suspectId, string otherSuspectId);
        IEnumerable<TestimonyStatement> FindConflictingStatements(string suspectId, string topic);
    }
}
