using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Services
{
    public class InvestigationContext : IInvestigationContext
    {
        private readonly IInvestigationRepository _repository;
        private readonly IContradictionEventPublisher _eventPublisher;

        public InvestigationContext(IInvestigationRepository repository, IContradictionEventPublisher eventPublisher)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public TestimonyStatement? GetTestimony(string statementId)
        {
            if (string.IsNullOrWhiteSpace(statementId))
            {
                return null;
            }

            return _repository.GetTestimonyAsync(statementId).Result;
        }

        public Evidence? GetEvidence(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId))
            {
                return null;
            }

            return _repository.GetEvidenceAsync(evidenceId).Result;
        }

        public DossierState? GetDossier(string suspectId)
        {
            if (string.IsNullOrWhiteSpace(suspectId))
            {
                return null;
            }

            return _repository.GetDossierAsync(suspectId).Result;
        }

        public IEnumerable<TestimonyStatement> GetAllTestimony()
        {
            return _repository.GetAllTestimonyAsync().Result;
        }

        public IEnumerable<Evidence> GetAllEvidence()
        {
            return _repository.GetAllEvidenceAsync().Result;
        }

        public IEnumerable<ContradictionResult> GetResolvedContradictions()
        {
            var allDossiers = _repository.GetAllDossiersAsync().Result;
            var resolvedContradictions = new List<ContradictionResult>();

            foreach (var dossier in allDossiers)
            {
                resolvedContradictions.AddRange(dossier.Contradictions.Where(c => c.Resolution.HasAnyResolution));
            }

            return resolvedContradictions;
        }

        public async Task UpdateTestimonyAsync(string statementId, string amendedText)
        {
            if (string.IsNullOrWhiteSpace(statementId) || string.IsNullOrWhiteSpace(amendedText))
            {
                throw new ArgumentException("Statement ID and amended text must be provided.");
            }

            var testimony = await _repository.GetTestimonyAsync(statementId);
            if (testimony == null)
            {
                throw new InvalidOperationException($"Testimony with ID {statementId} not found");
            }

            testimony.AmendedText = amendedText;
            await _repository.SaveTestimonyAsync(testimony);

            var dossier = await _repository.GetDossierAsync(testimony.SuspectId);
            if (dossier != null)
            {
                if (dossier.TestimonyIds.Contains(statementId))
                {
                    dossier.LastUpdated = DateTime.UtcNow;
                    await _repository.SaveDossierAsync(dossier);
                    await _eventPublisher.PublishDossierUpdatedAsync(dossier.SuspectId);
                }
            }
        }

        public async Task AddEvidenceAsync(Evidence evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            await _repository.SaveEvidenceAsync(evidence);
            await _eventPublisher.PublishEvidenceUnlockedAsync(evidence.Id);
        }

        public async Task AddCrossReferenceAsync(CrossReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            await AddCrossReferenceToDossier(reference.FromSuspectId, reference);
            await AddCrossReferenceToDossier(reference.ToSuspectId, reference);

            await _eventPublisher.PublishCrossReferenceCreatedAsync(reference);
        }

        private async Task AddCrossReferenceToDossier(string suspectId, CrossReference reference)
        {
            var dossier = await _repository.GetDossierAsync(suspectId);
            if (dossier == null)
            {
                return;
            }

            var exists = dossier.Relationships.Any(r =>
                r.FromSuspectId == reference.FromSuspectId &&
                r.ToSuspectId == reference.ToSuspectId &&
                r.RelationshipType == reference.RelationshipType);

            if (!exists)
            {
                dossier.Relationships.Add(reference);
                await _repository.SaveDossierAsync(dossier);
                await _eventPublisher.PublishDossierUpdatedAsync(suspectId);
            }
        }

        public async Task MarkContradictionResolvedAsync(string contradictionId, ContradictionResolution resolution)
        {
            if (string.IsNullOrWhiteSpace(contradictionId) || resolution == null)
            {
                throw new ArgumentException("Contradiction ID and resolution must be provided");
            }

            var allDossiers = await _repository.GetAllDossiersAsync();
            var affectedDossier = allDossiers.FirstOrDefault(d =>
                d.Contradictions.Any(c => c.ContradictionId == contradictionId));

            if (affectedDossier == null)
            {
                throw new InvalidOperationException($"Contradiction {contradictionId} not found in any dossier");
            }

            var contradiction = affectedDossier.Contradictions.First(c => c.ContradictionId == contradictionId);
            contradiction.Resolution = resolution;

            await _repository.SaveDossierAsync(affectedDossier);
            await _eventPublisher.PublishContradictionResolvedAsync(contradictionId, resolution);
            await _eventPublisher.PublishDossierUpdatedAsync(affectedDossier.SuspectId);
        }
    }
}
