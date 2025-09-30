using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Services
{
    public class ContradictionService : IContradictionService
    {
        private readonly IEnumerable<IContradictionDetector> _detectors;
        private readonly IInvestigationContext _context;
        private readonly IContradictionEventPublisher _eventPublisher;

        public ContradictionService(
            IEnumerable<IContradictionDetector> detectors,
            IInvestigationContext context,
            IContradictionEventPublisher eventPublisher)
        {
            _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task<ContradictionResult> CheckContradictionAsync(IContradictionQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var detector = _detectors.FirstOrDefault(d => d.CanHandle(query));
            if (detector == null)
            {
                return new ContradictionResult
                {
                    IsContradiction = false,
                    Description = $"No detector found for query type {query.GetType().Name}",
                    ContradictionId = $"no_detector_{query.QueryId}"
                };
            }

            var result = await detector.DetectAsync(query, _context);

            if (result.IsContradiction)
            {
                await _eventPublisher.PublishContradictionFoundAsync(result);

                foreach (var suspectId in result.AffectedSuspects)
                {
                    await AddContradictionToDossier(suspectId, result);
                }
            }

            return result;
        }

        public async Task<IEnumerable<ContradictionResult>> GetPossibleContradictionsAsync(string suspectId)
        {
            if (string.IsNullOrWhiteSpace(suspectId))
            {
                return Enumerable.Empty<ContradictionResult>();
            }

            var dossier = _context.GetDossier(suspectId);
            if (dossier == null)
            {
                return Enumerable.Empty<ContradictionResult>();
            }

            var possibleContradictions = new List<ContradictionResult>();

            foreach (var testimony in dossier.Testimony)
            {
                var allEvidence = _context.GetAllEvidence();
                foreach (var evidence in allEvidence)
                {
                    foreach (ContradictionType type in Enum.GetValues<ContradictionType>())
                    {
                        var query = new TestimonyVsEvidenceQuery(testimony.Id, evidence.Id, type);
                        var result = await CheckContradictionAsync(query);

                        if (result.IsContradiction)
                        {
                            possibleContradictions.Add(result);
                        }
                    }
                }
            }

            return possibleContradictions;
        }

        public async Task<ContradictionResult> ApplyResolutionAsync(ContradictionResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.IsContradiction)
            {
                throw new ArgumentException("Cannot apply resolution to non-contradiction");
            }

            var resolution = result.Resolution;

            if (!string.IsNullOrEmpty(resolution.AmendedTestimony))
            {
                var affectedSuspect = result.AffectedSuspects.FirstOrDefault();
                if (affectedSuspect != null)
                {
                    var dossier = _context.GetDossier(affectedSuspect);
                    if (dossier != null && dossier.Testimony.Any())
                    {
                        var latestTestimony = dossier.Testimony.OrderByDescending(t => t.Timestamp).FirstOrDefault();
                        if (latestTestimony != null)
                        {
                            await _context.UpdateTestimonyAsync(latestTestimony.Id, resolution.AmendedTestimony);
                        }
                    }
                }
            }

            foreach (var evidenceId in resolution.NewEvidenceIds)
            {
                var evidence = _context.GetEvidence(evidenceId);
                if (evidence != null)
                {
                    await _context.AddEvidenceAsync(evidence);
                }
            }

            foreach (var crossRef in resolution.CrossReferences)
            {
                await _context.AddCrossReferenceAsync(crossRef);
            }

            await _context.MarkContradictionResolvedAsync(result.ContradictionId, resolution);

            return result;
        }

        public Task<bool> IsContradictionResolvedAsync(string contradictionId)
        {
            if (string.IsNullOrWhiteSpace(contradictionId))
            {
                return Task.FromResult(false);
            }

            var resolvedContradictions = _context.GetResolvedContradictions();
            return Task.FromResult(resolvedContradictions.Any(c => c.ContradictionId == contradictionId));
        }

        public async Task<IEnumerable<ContradictionResult>> GetUnresolvedContradictionsAsync()
        {
            var allDossiers = _context.GetDossier("");
            return Enumerable.Empty<ContradictionResult>();
        }

        private async Task AddContradictionToDossier(string suspectId, ContradictionResult contradiction)
        {
            var dossier = _context.GetDossier(suspectId);
            if (dossier == null)
            {
                return;
            }

            var exists = dossier.Contradictions.Any(c => c.ContradictionId == contradiction.ContradictionId);
            if (!exists)
            {
                dossier.Contradictions.Add(contradiction);
                await _eventPublisher.PublishDossierUpdatedAsync(suspectId);
            }
        }
    }
}
