using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions.Detectors
{
    public class IdentityContradictionDetector : IContradictionDetector
    {
        private class IdentityInfo
        {
            public string Name { get; }
            public string FieldName { get; }
            public string Context { get; }
            public bool IsDenial { get; }

            public IdentityInfo(string name, string fieldName, string context, bool isDenial = false)
            {
                Name = name;
                FieldName = fieldName;
                Context = context;
                IsDenial = isDenial;
            }
        }

        private readonly ITestimonyQueryService _testimonyQuery;

        public ContradictionType HandledType => ContradictionType.Identity;

        public IdentityContradictionDetector(ITestimonyQueryService testimonyQuery)
        {
            _testimonyQuery = testimonyQuery ?? throw new ArgumentNullException(nameof(testimonyQuery));
        }

        public bool CanHandle(IContradictionQuery query)
        {
            return query.ExpectedType == ContradictionType.Identity &&
                (query is ITestimonyVsEvidenceQuery || query is IEvidenceVsEvidenceQuery);
        }

        public Task<ContradictionResult> DetectAsync(IContradictionQuery query, IInvestigationContext context)
        {
            if (query is ITestimonyVsEvidenceQuery testimonyQuery)
            {
                return DetectTestimonyVsEvidence(testimonyQuery, context);
            }

            if (query is IEvidenceVsEvidenceQuery evidenceQuery)
            {
                return DetectEvidenceVsEvidence(evidenceQuery, context);
            }

            return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Query type not supported"));
        }

        private Task<ContradictionResult> DetectTestimonyVsEvidence(
            ITestimonyVsEvidenceQuery query,
            IInvestigationContext context)
        {
            var testimony = context.GetTestimony(query.TestimonyStatementId);
            var evidence = context.GetEvidence(query.EvidenceId);

            if (testimony == null || evidence == null)
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Testimony or evidence not found"));
            }

            var testimonyIdentities = ExtractIdentitiesFromTestimony(testimony);
            if (!testimonyIdentities.Any())
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "No identity information in testimony"));
            }

            var evidenceIdentities = ExtractIdentitiesFromEvidence(evidence);
            if (!evidenceIdentities.Any())
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "No identity information in evidence"));
            }

            foreach (var testimonyIdentity in testimonyIdentities)
            {
                foreach (var evidenceIdentity in evidenceIdentities)
                {
                    if (!testimonyIdentity.IsDenial &&
                        AreIdentitiesConflicting(testimonyIdentity.Name, evidenceIdentity.Name))
                    {
                        return Task.FromResult(CreateContradictionResult(
                            query.QueryId,
                            testimony,
                            evidence,
                            testimonyIdentity,
                            evidenceIdentity));
                    }

                    if (testimonyIdentity.IsDenial &&
                        !AreIdentitiesConflicting(testimonyIdentity.Name, evidenceIdentity.Name))
                    {
                        return Task.FromResult(CreateContradictionResult(
                            query.QueryId,
                            testimony,
                            evidence,
                            testimonyIdentity,
                            evidenceIdentity));
                    }
                }
            }

            return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Identities are consistent"));
        }

        private Task<ContradictionResult> DetectEvidenceVsEvidence(
            IEvidenceVsEvidenceQuery query,
            IInvestigationContext context)
        {
            var evidence1 = context.GetEvidence(query.PrimaryEvidenceId);
            var evidence2 = context.GetEvidence(query.SecondaryEvidenceId);

            if (evidence1 == null || evidence2 == null)
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Evidence not found"));
            }

            var identities1 = ExtractIdentitiesFromEvidence(evidence1);
            var identities2 = ExtractIdentitiesFromEvidence(evidence2);

            if (!identities1.Any() || !identities2.Any())
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Insufficient identity information"));
            }

            foreach (var identity1 in identities1)
            {
                foreach (var identity2 in identities2)
                {
                    if (AreIdentitiesConflicting(identity1.Name, identity2.Name))
                    {
                        return Task.FromResult(CreateEvidenceContradictionResult(
                            query.QueryId,
                            evidence1,
                            evidence2,
                            identity1,
                            identity2));
                    }
                }
            }

            return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Evidence identities are consistent"));
        }

        private IdentityInfo[] ExtractIdentitiesFromTestimony(TestimonyStatement testimony)
        {
            var identities = new List<IdentityInfo>();

            if (testimony.Metadata.TryGetValue("claimed_identity", out var identity))
            {
                var name = identity?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    identities.Add(new IdentityInfo(name, "claimed_identity", testimony.CurrentText));
                }
            }

            if (testimony.Metadata.TryGetValue("denied_identity", out var deniedIdentity))
            {
                var name = deniedIdentity?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    identities.Add(new IdentityInfo(name, "denied_identity", testimony.CurrentText, isDenial: true));
                }
            }

            return identities.ToArray();
        }

        private IdentityInfo[] ExtractIdentitiesFromEvidence(Evidence evidence)
        {
            var identities = new List<IdentityInfo>();

            var nameFields = new[]
            {
                "full_name", "name", "passenger_name", "occupant_name",
                "subject_name", "owner_name", "holder_name"
            };

            foreach (var field in nameFields)
            {
                if (evidence.Content.TryGetValue(field, out var value))
                {
                    var nameStr = value?.ToString();
                    if (!string.IsNullOrEmpty(nameStr))
                    {
                        identities.Add(new IdentityInfo(nameStr, field, evidence.Title));
                    }
                }
            }

            return identities.ToArray();
        }

        private bool AreIdentitiesConflicting(string name1, string name2)
        {
            var normalized1 = NormalizeName(name1);
            var normalized2 = NormalizeName(name2);

            if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (normalized1.Contains(normalized2, StringComparison.OrdinalIgnoreCase) ||
                normalized2.Contains(normalized1, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (AreInitialsMatching(normalized1, normalized2))
            {
                return false;
            }

            return true;
        }

        private bool AreInitialsMatching(string name1, string name2)
        {
            var parts1 = name1.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
            var parts2 = name2.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts1.Length != parts2.Length)
            {
                return false;
            }

            for (int i=0; i<parts1.Length; i++)
            {
                var part1 = parts1[i];
                var part2 = parts2[i];

                if (part1.Length > 1 && part2.Length > 1)
                {
                    if (!part1.Equals(part2, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else if (part1.Length == 1 || part2.Length == 1)
                {
                    var initial = part1.Length == 1 ? part1[0] : part2[0];
                    var fullName = part1.Length == 1 ? part2 : part1;

                    if (char.ToUpper(initial) != char.ToUpper(fullName[0]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private string NormalizeName(string name)
        {
            return name.Trim();
        }

        private ContradictionResult CreateContradictionResult(
            string queryId,
            TestimonyStatement testimony,
            Evidence evidence,
            IdentityInfo testimonyIdentity,
            IdentityInfo evidenceIdentity)
        {
            var contradictionId = $"co_{testimony.SuspectId.Split('_').Last()}_identity_{Guid.NewGuid().ToString("N").Substring(0, 2)}";

            var description = testimonyIdentity.IsDenial
                ? $"Suspect denies identity '{testimonyIdentity.Name}' but evidence shows '{evidenceIdentity.Name}'"
                : $"Testimony claims identity '{testimonyIdentity.Name}' but evidence shows '{evidenceIdentity.Name}'";

            return new ContradictionResult
            {
                IsContradiction = true,
                Type = ContradictionType.Identity,
                ContradictionId = contradictionId,
                Description = description,
                AffectedSuspects = new() { testimony.SuspectId },
                RelatedEvidence = new() { evidence.Id },
                Resolution = new ContradictionResolution
                {

                }
            };
        }

        private ContradictionResult CreateEvidenceContradictionResult(
            string queryId,
            Evidence evidence1,
            Evidence evidence2,
            IdentityInfo identity1,
            IdentityInfo identity2)
        {
            var contradictionId = $"co_evidence_identity_{Guid.NewGuid().ToString("N").Substring(0, 2)}";

            var description = $"Evidence conflict: '{evidence1.Title}' shows '{identity1.Name}' but '{evidence2.Title}' shows '{identity2.Name}'";

            return new ContradictionResult
            {
                IsContradiction = true,
                Type = ContradictionType.Identity,
                ContradictionId = contradictionId,
                Description = description,
                RelatedEvidence = new() { evidence1.Id, evidence2.Id }
            };
        }

        private ContradictionResult CreateNoContradictionResult(string queryId, string reason)
        {
            return new ContradictionResult
            {
                IsContradiction = false,
                Type = ContradictionType.Identity,
                Description = reason,
                ContradictionId = $"no_contradiction_{queryId}"
            };
        }
    }
}
