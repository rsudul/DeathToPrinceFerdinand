using System.Collections.Generic;
using DeathToPrinceFerdinand.scripts.Core.Models;


namespace DeathToPrinceFerdinand.scripts.Core.Contradictions
{
    public interface IEvidenceQueryService
    {
        IEnumerable<Evidence> FindByCategory(string category);
        IEnumerable<Evidence> FindByTimeRange(string startTime, string endTime);
        IEnumerable<Evidence> FindByLocation(string location);
        IEnumerable<Evidence> FindReferencingSuspect(string suspectId);
        IEnumerable<Evidence> FindByContentField(string fieldName, object value);
    }
}
