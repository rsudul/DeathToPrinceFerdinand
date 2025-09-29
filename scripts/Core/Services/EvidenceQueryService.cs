using System;
using System.Collections.Generic;
using System.Linq;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Services
{
    public class EvidenceQueryService : IEvidenceQueryService
    {
        private readonly IInvestigationRepository _repository;

        public EvidenceQueryService(IInvestigationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IEnumerable<Evidence> FindByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Enumerable.Empty<Evidence>();
            }

            var allEvidence = _repository.GetAllEvidenceAsync().Result;
            return allEvidence.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Evidence> FindByTimeRange(string startTime, string endTime)
        {
            if (string.IsNullOrWhiteSpace(startTime) || string.IsNullOrWhiteSpace(endTime))
            {
                return Enumerable.Empty<Evidence>();
            }

            var allEvidence = _repository.GetAllEvidenceAsync().Result;

            return allEvidence.Where(e =>
            {
                var timeFields = new[] { "time", "arrival_time", "departure_time", "timestamp" };
                return timeFields.Any(field => e.Content.ContainsKey(field));
            });
        }

        public IEnumerable<Evidence> FindByLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return Enumerable.Empty<Evidence>();
            }

            var allEvidence = _repository.GetAllEvidenceAsync().Result;

            return allEvidence.Where(e =>
            {
                var locationFields = new[] { "location", "destination", "departure", "place", "address" };
                return locationFields.Any(field =>
                    e.Content.ContainsKey(field) &&
                    e.Content[field]?.ToString()?.Contains(location, StringComparison.OrdinalIgnoreCase) == true) ||
                    e.Title.Contains(location, StringComparison.OrdinalIgnoreCase);
            });
        }

        public IEnumerable<Evidence> FindReferencingSuspect(string suspectId)
        {
            if (string.IsNullOrWhiteSpace(suspectId))
            {
                return Enumerable.Empty<Evidence>();
            }

            var allEvidence = _repository.GetAllEvidenceAsync().Result;
            var dossier = _repository.GetDossierAsync(suspectId).Result;

            if (dossier == null)
            {
                return Enumerable.Empty<Evidence>();
            }

            var searchTerms = new List<string> { dossier.Name };
            if (!string.IsNullOrEmpty(dossier.Alias))
            {
                searchTerms.Add(dossier.Alias);
            }

            return allEvidence.Where(e =>
                searchTerms.Any(term =>
                    e.Content.Values.Any(value =>
                        value?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                    e.Title.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        public IEnumerable<Evidence> FindByContentField(string fieldName, object value)
        {
            if (string.IsNullOrWhiteSpace(fieldName) || value == null)
            {
                return Enumerable.Empty<Evidence>();
            }

            var allEvidence = _repository.GetAllEvidenceAsync().Result;
            var valueString = value.ToString();

            return allEvidence.Where(e =>
                e.Content.ContainsKey(fieldName) &&
                e.Content[fieldName]?.ToString()?.Equals(valueString, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
