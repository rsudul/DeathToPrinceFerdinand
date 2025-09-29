using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Core.Contradictions.Detectors
{
    public class TimelineContradictionsDetector : IContradictionDetector
    {
        private class TimeInfo
        {
            public TimeSpan Time { get; }
            public string FieldName { get; }
            public string Context { get; }

            public TimeInfo(TimeSpan time, string fieldName, string context)
            {
                Time = time;
                FieldName = fieldName;
                Context = context;
            }
        }

        private readonly ITestimonyQueryService _testimonyQuery;

        private const int TimeToleranceMinutes = 10;

        public ContradictionType HandledType => ContradictionType.Timeline;

        public TimelineContradictionsDetector(ITestimonyQueryService testimonyQuery)
        {
            _testimonyQuery = testimonyQuery ?? throw new ArgumentNullException(nameof(testimonyQuery));
        }

        public bool CanHandle(IContradictionQuery query)
        {
            return query.ExpectedType == ContradictionType.Timeline &&
                (query is ITestimonyVsEvidenceQuery || query is IEvidenceVsEvidenceQuery);
        }

        public async Task<ContradictionResult> DetectAsync(IContradictionQuery query, IInvestigationContext context)
        {
            if (query is ITestimonyVsEvidenceQuery testimonyQuery)
            {
                return await DetectTestimonyVsEvidenceAsync(testimonyQuery, context);
            }

            if (query is IEvidenceVsEvidenceQuery evidenceQuery)
            {
                return await DetectEvidenceVsEvidenceAsync(evidenceQuery, context);
            }

            return CreateNoContradictionResult(query.QueryId, "Query type not supported");
        }

        private async Task<ContradictionResult> DetectTestimonyVsEvidenceAsync(
            ITestimonyVsEvidenceQuery query,
            IInvestigationContext context)
        {
            var testimony = context.GetTestimony(query.TestimonyStatementId);
            var evidence = context.GetEvidence(query.EvidenceId);

            if (testimony == null || evidence == null)
            {
                return CreateNoContradictionResult(query.QueryId, "Testimony or evidence not found");
            }

            var testimonyTimes = ExtractTimesFromText(testimony.CurrentText);
            if (!testimonyTimes.Any())
            {
                return CreateNoContradictionResult(query.QueryId, "No time information in testimony");
            }

            var evidenceTimes = ExtractTimesFromEvidence(evidence);
            if (!evidenceTimes.Any())
            {
                return CreateNoContradictionResult(query.QueryId, "No time information in evidence");
            }

            foreach (var testimonyTime in testimonyTimes)
            {
                foreach (var evidenceTime in evidenceTimes)
                {
                    if (AreTimesConflicting(testimonyTime.Time, evidenceTime.Time))
                    {
                        return await CreateContradictionResultAsync(
                            query.QueryId,
                            testimony,
                            evidence,
                            testimonyTime,
                            evidenceTime,
                            context);
                    }
                }
            }

            return CreateNoContradictionResult(query.QueryId, "Times are consistent");
        }

        private async Task<ContradictionResult> DetectEvidenceVsEvidenceAsync(
            IEvidenceVsEvidenceQuery query,
            IInvestigationContext context)
        {
            var evidence1 = context.GetEvidence(query.PrimaryEvidenceId);
            var evidence2 = context.GetEvidence(query.SecondaryEvidenceId);

            if (evidence1 == null || evidence2 == null)
            {
                return CreateNoContradictionResult(query.QueryId, "Evidence not found");
            }

            var times1 = ExtractTimesFromEvidence(evidence1);
            var times2 = ExtractTimesFromEvidence(evidence2);

            if (!times1.Any() || !times2.Any())
            {
                return CreateNoContradictionResult(query.QueryId, "Insufficient time information");
            }

            foreach (var time1 in times1)
            {
                foreach (var time2 in times2)
                {
                    if (AreTimesConflicting(time1.Time, time2.Time))
                    {
                        return CreateEvidenceContradictionResult(
                            query.QueryId,
                            evidence1,
                            evidence2,
                            time1,
                            time2);
                    }
                }
            }

            return CreateNoContradictionResult(query.QueryId, "Evidence times are consistent");
        }

        private TimeInfo[] ExtractTimesFromText(string text)
        {
            var times = new List<TimeInfo>();

            var timePatterns = new[]
            {
                @"(?:at|around|about|approximately)\s+(\d{1,2}):?(\d{2})?\s*(AM|PM|am|pm)?",
                @"(?:at|around|about|approximately)\s+(noon|midnight)",
                @"(\d{1,2}):(\d{2})\s*(AM|PM|am|pm)?"
            };

            foreach (var pattern in timePatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var timeInfo = ParseTimeFromMatch(match, text);
                    if (timeInfo != null)
                    {
                        times.Add(timeInfo);
                    }
                }
            }

            return times.ToArray();
        }

        private TimeInfo[] ExtractTimesFromEvidence(Evidence evidence)
        {
            var times = new List<TimeInfo>();

            var timeFields = new[]
            {
                "time", "arrival_time", "departure_time",
                "timestamp", "scheduled_time", "actual_time"
            };

            foreach (var field in timeFields)
            {
                if (evidence.Content.TryGetValue(field, out var value))
                {
                    var timeStr = value?.ToString();
                    if (!string.IsNullOrEmpty(timeStr))
                    {
                        var timeInfo = ParseTimeString(timeStr, field);
                        if (timeInfo != null)
                        {
                            times.Add(timeInfo);
                        }
                    }
                }
            }

            var titleTimes = ExtractTimesFromText(evidence.Title);
            times.AddRange(titleTimes);

            return times.ToArray();
        }

        private TimeInfo? ParseTimeFromMatch(Match match, string sourceText)
        {
            try
            {
                var groups = match.Groups;

                if (groups[1].Value.Equals("noon", StringComparison.OrdinalIgnoreCase))
                {
                    return new TimeInfo(new TimeSpan(12, 0, 0), "noon", sourceText);
                }
                if (groups[1].Value.Equals("midnight", StringComparison.OrdinalIgnoreCase))
                {
                    return new TimeInfo(new TimeSpan(0, 0, 0), "midnight", sourceText);
                }

                if (!int.TryParse(groups[1].Value, out int hour))
                {
                    return null;
                }

                int minute = 0;
                if (groups.Count > 2 && !string.IsNullOrEmpty(groups[2].Value))
                {
                    int.TryParse(groups[2].Value, out minute);
                }

                var ampm = groups.Count > 3 ? groups[3].Value : "";
                if (!string.IsNullOrEmpty(ampm))
                {
                    if (ampm.Equals("PM", StringComparison.OrdinalIgnoreCase) && hour < 12)
                    {
                        hour += 12;
                    }
                    if (ampm.Equals("AM", StringComparison.OrdinalIgnoreCase) && hour == 12)
                    {
                        hour = 0;
                    }
                }

                return new TimeInfo(new TimeSpan(hour, minute, 0), match.Value, sourceText);
            }
            catch
            {
                return null;
            }
        }

        private TimeInfo? ParseTimeString(string timeStr, string fieldName)
        {
            try
            {
                if (TimeSpan.TryParse(timeStr, out var timeSpan))
                {
                    return new TimeInfo(timeSpan, fieldName, timeStr);
                }

                if (DateTime.TryParse(timeStr, out var dateTime))
                {
                    return new TimeInfo(dateTime.TimeOfDay, fieldName, timeStr);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool AreTimesConflicting(TimeSpan time1, TimeSpan time2)
        {
            var difference = Math.Abs((time1 - time2).TotalMinutes);
            return difference > TimeToleranceMinutes;
        }

        private Task<ContradictionResult> CreateContradictionResultAsync(
            string queryId,
            TestimonyStatement testimony,
            Evidence evidence,
            TimeInfo testimonyTime,
            TimeInfo evidenceTime,
            IInvestigationContext context)
        {
            var contradictionId = $"co_{testimony.SuspectId.Split('_').Last()}_timeline_{Guid.NewGuid().ToString("N").Substring(0, 2)}";

            var description = $"Testimony states '{testimonyTime.Context}' but evidence shows '{evidenceTime.Context}'";

            return Task.FromResult<ContradictionResult>(new ContradictionResult
            {
                IsContradiction = true,
                Type = ContradictionType.Timeline,
                ContradictionId = contradictionId,
                Description = description,
                AffectedSuspects = new() { testimony.SuspectId },
                RelatedEvidence = new() { evidence.Id },
                Resolution = new ContradictionResolution
                {

                }
            });
        }

        private ContradictionResult CreateEvidenceContradictionResult(
            string queryId,
            Evidence evidence1,
            Evidence evidence2,
            TimeInfo time1,
            TimeInfo time2)
        {
            var contradictionId = $"co_evidence_timeline_{Guid.NewGuid().ToString("N").Substring(0, 2)}";

            var description = $"Evidence conflict: '{evidence1.Title}' shows '{time1.Context}' but '{evidence2.Title}' shows '{time2.Context}'";

            return new ContradictionResult
            {
                IsContradiction = true,
                Type = ContradictionType.Timeline,
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
                Type = ContradictionType.Timeline,
                Description = reason,
                ContradictionId = $"no_contradiction_{queryId}"
            };
        }
    }
}
