using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Services
{
    public class ContradictionEventPublisher : IContradictionEventPublisher
    {
        public async Task PublishContradictionFoundAsync(ContradictionResult result)
        {
            Console.WriteLine($"Contradiction found: {result.ContradictionId}");
            await Task.CompletedTask;
        }

        public async Task PublishContradictionResolvedAsync(string contradictionId, ContradictionResolution resolution)
        {
            Console.WriteLine($"Contradiction resolved: {contradictionId}");
            await Task.CompletedTask;
        }

        public async Task PublishDossierUpdatedAsync(string suspectId)
        {
            Console.WriteLine($"Dossier updated: {suspectId}");
            await Task.CompletedTask;
        }

        public async Task PublishEvidenceUnlockedAsync(string evidenceId)
        {
            Console.WriteLine($"Evidence unlocked: {evidenceId}");
            await Task.CompletedTask;
        }

        public async Task PublishCrossReferenceCreatedAsync(CrossReference reference)
        {
            Console.WriteLine($"Cross-reference created: {reference.FromSuspectId} -> {reference.ToSuspectId}");
            await Task.CompletedTask;
        }
    }
}
