using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;
using Godot;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions.Detectors
{
    public class LocationContradictionDetector : IContradictionDetector
    {
        private class LocationInfo
        {
            public string Location { get; }
            public string FieldName { get; }
            public string Context { get; }

            public LocationInfo(string location, string fieldName, string context)
            {
                Location = location;
                FieldName = fieldName;
                Context = context;
            }
        }

        private readonly ITestimonyQueryService _testimonyQuery;

        public ContradictionType HandledType => ContradictionType.Location;

        public LocationContradictionDetector(ITestimonyQueryService testimonyQuery)
        {
            _testimonyQuery = testimonyQuery ?? throw new ArgumentNullException(nameof(testimonyQuery));
        }

        public bool CanHandle(IContradictionQuery query)
        {
            return query.ExpectedType == ContradictionType.Location &&
                (query is ITestimonyVsEvidenceQuery || query is IEvidenceVsEvidenceQuery);
        }

        public Task<ContradictionResult> DetectAsync(IContradictionQuery query, IInvestigationContext context)
        {
            if (query is ITestimonyVsEvidenceQuery testimonyQuery)
            {
                return DetectTestimonyVsEvidenceAsync(testimonyQuery, context);
            }

            if (query is IEvidenceVsEvidenceQuery evidenceQuery)
            {
                return DetectEvidenceVsEvidenceAsync(evidenceQuery, context);
            }

            return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Query type not supported"));
        }

        private Task<ContradictionResult> DetectTestimonyVsEvidenceAsync(
            ITestimonyVsEvidenceQuery query,
            IInvestigationContext context)
        {
            var testimony = context.GetTestimony(query.TestimonyStatementId);
            var evidence = context.GetEvidence(query.EvidenceId);

            if (testimony == null || evidence == null)
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Testimony or evidence not found"));
            }

            var testimonyLocations = ExtractLocationsFromTestimony(testimony);
            if (!testimonyLocations.Any())
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "No location information in testimony"));
            }

            var evidenceLocations = ExtractLocationsFromEvidence(evidence);
            if (!evidenceLocations.Any())
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "No location information in evidence"));
            }

            foreach (var testimonyLocation in testimonyLocations)
            {
                foreach (var evidenceLocation in evidenceLocations)
                {
                    if (AreLocationsConflicting(testimonyLocation.Location, evidenceLocation.Location))
                    {
                        return Task.FromResult(CreateContradictionResult(
                            query.QueryId,
                            testimony,
                            evidence,
                            testimonyLocation,
                            evidenceLocation,
                            context));
                    }
                }
            }

            return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Locations are consistent"));
        }

        private Task<ContradictionResult> DetectEvidenceVsEvidenceAsync(
            IEvidenceVsEvidenceQuery query,
            IInvestigationContext context)
        {
            var evidence1 = context.GetEvidence(query.PrimaryEvidenceId);
            var evidence2 = context.GetEvidence(query.SecondaryEvidenceId);

            if (evidence1 == null || evidence2 == null)
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Evidence not found"));
            }

            var locations1 = ExtractLocationsFromEvidence(evidence1);
            var locations2 = ExtractLocationsFromEvidence(evidence2);

            if (!locations1.Any() || !locations2.Any())
            {
                return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Insufficient location information"));
            }

            foreach (var location1 in locations1)
            {
                foreach (var location2 in locations2)
                {
                    if (AreLocationsConflicting(location1.Location, location2.Location))
                    {
                        return Task.FromResult(CreateEvidenceContradictionResult(
                            query.QueryId,
                            evidence1,
                            evidence2,
                            location1,
                            location2));
                    }
                }
            }

            return Task.FromResult(CreateNoContradictionResult(query.QueryId, "Evidence locations are consistent"));
        }

        private LocationInfo[] ExtractLocationsFromTestimony(TestimonyStatement testimony)
        {
            var locations = new List<LocationInfo>();

            if (testimony.Metadata.TryGetValue("claimed_location", out var location1))
            {
                var loc = location1?.ToString();
                if (!string.IsNullOrEmpty(loc))
                {
                    locations.Add(new LocationInfo(loc, "claimed_location", testimony.CurrentText));
                }
            }

            if (testimony.Metadata.TryGetValue("claimed_location_2", out var location2))
            {
                var loc = location2?.ToString();
                if (!string.IsNullOrEmpty(loc))
                {
                    locations.Add(new LocationInfo(loc, "claimed_location_2", testimony.CurrentText));
                }
            }

            return locations.ToArray();
        }

        private LocationInfo[] ExtractLocationsFromEvidence(Evidence evidence)
        {
            var locations = new List<LocationInfo>();

            var locationFields = new[]
            {
                "location", "place", "destination", "departure",
                "address", "venue", "site", "meeting_place"
            };

            foreach (var field in locationFields)
            {
                if (evidence.Content.TryGetValue(field, out var value))
                {
                    var locationStr = value?.ToString();
                    if (!string.IsNullOrEmpty(locationStr))
                    {
                        locations.Add(new LocationInfo(locationStr, field, evidence.Title));
                    }
                }
            }

            return locations.ToArray();
        }

        private bool AreLocationsConflicting(string location1, string location2)
        {
            var normalized1 = NormalizeLocation(location1);
            var normalized2 = NormalizeLocation(location2);

            if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (normalized1.Contains(normalized2, StringComparison.OrdinalIgnoreCase) ||
                normalized2.Contains(normalized1, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private string NormalizeLocation(string location)
        {
            var normalized = location.Trim();

            var descriptors = new[] { "Station", "Café", "Cafe", "Gate", "Hall", "Building", "Factory", "Hotel", "Restaurant" };
            foreach (var descriptor in descriptors)
            {
                if (normalized.EndsWith(descriptor, StringComparison.OrdinalIgnoreCase))
                {
                    var lastSpace = normalized.LastIndexOf(' ');
                    if (lastSpace > 0)
                    {
                        normalized = normalized.Substring(0, lastSpace).Trim();
                    }
                }
            }

            return normalized;
        }

        private ContradictionResult CreateContradictionResult(
            string queryId,
            TestimonyStatement testimony,
            Evidence evidence,
            LocationInfo testimonyLocation,
            LocationInfo evidenceLocation,
            IInvestigationContext context)
        {
            var contradictionId = $"co_{testimony.SuspectId.Split('_').Last()}_location_{Guid.NewGuid().ToString("N").Substring(0, 2)}";

            var description = $"Testimony claims '{testimonyLocation.Location}' but evidence shows '{evidenceLocation.Location}'";

            return new ContradictionResult
            {
                IsContradiction = true,
                Type = ContradictionType.Location,
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
            LocationInfo location1,
            LocationInfo location2)
        {
            var contradictionId = $"co_evidence_location_{Guid.NewGuid().ToString("N").Substring(0, 2)}";

            var description = $"Evidence conflict: '{evidence1.Title}' shows '{location1.Location}' but '{evidence2.Title}' shows '{location2.Location}'";

            return new ContradictionResult
            {
                IsContradiction = true,
                Type = ContradictionType.Location,
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
                Type = ContradictionType.Location,
                Description = reason,
                ContradictionId = $"no_contradiction_{queryId}"
            };
        }
    }
}
