using System;
using System.Collections.Generic;
using System.Linq;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Services
{
    public class TestimonyQueryService : ITestimonyQueryService
    {
        private readonly IInvestigationRepository _repository;

        public TestimonyQueryService(IInvestigationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IEnumerable<TestimonyStatement> FindByTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return Enumerable.Empty<TestimonyStatement>();
            }

            var allTestimony = _repository.GetAllTestimonyAsync().Result;
            return allTestimony.Where(t =>
                t.CurrentText.Contains(topic, StringComparison.OrdinalIgnoreCase) ||
                t.Metadata.ContainsKey("topic") && t.Metadata["topic"].ToString()?.Contains(topic, StringComparison.OrdinalIgnoreCase) == true);
        }

        public IEnumerable<TestimonyStatement> FindBySuspect(string suspectId)
        {
            if (string.IsNullOrWhiteSpace(suspectId))
            {
                return Enumerable.Empty<TestimonyStatement>();
            }

            var allTestimony = _repository.GetAllTestimonyAsync().Result;
            return allTestimony.Where(t => t.SuspectId == suspectId);
        }

        public IEnumerable<TestimonyStatement> FindByKeywords(params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                return Enumerable.Empty<TestimonyStatement>();
            }

            var allTestimony = _repository.GetAllTestimonyAsync().Result;
            return allTestimony.Where(t =>
                keywords.Any(keyword =>
                    t.CurrentText.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        public bool HasSuspectMentioned(string suspectId, string topic)
        {
            if (string.IsNullOrWhiteSpace(suspectId) || string.IsNullOrWhiteSpace(topic))
            {
                return false;
            }

            return FindBySuspect(suspectId)
                .Any(t => t.CurrentText.Contains(topic, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasSuspectClaimedRelationship(string suspectId, string otherSuspectId)
        {
            if (string.IsNullOrWhiteSpace(suspectId) || string.IsNullOrWhiteSpace(otherSuspectId))
            {
                return false;
            }

            var suspectTestimony = FindBySuspect(suspectId);

            var otherDossier = _repository.GetDossierAsync(otherSuspectId).Result;
            if (otherDossier == null)
            {
                return false;
            }

            var searchTerms = new List<string> { otherDossier.Name };
            if (!string.IsNullOrEmpty(otherDossier.Alias))
            {
                searchTerms.Add(otherDossier.Alias);
            }
            if (!string.IsNullOrEmpty(otherDossier.Codename))
            {
                searchTerms.Add(otherDossier.Codename);
            }

            return suspectTestimony.Any(t =>
                searchTerms.Any(term =>
                    t.CurrentText.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        public IEnumerable<TestimonyStatement> FindConflictingStatements(string suspectId, string topic)
        {
            if (string.IsNullOrWhiteSpace(suspectId) || string.IsNullOrWhiteSpace(topic))
            {
                return Enumerable.Empty<TestimonyStatement>();
            }

            var suspectTestimony = FindBySuspect(suspectId);
            var topicStatements = suspectTestimony.Where(t =>
                t.CurrentText.Contains(topic, StringComparison.OrdinalIgnoreCase)).ToList();

            return topicStatements;
        }
    }
}
