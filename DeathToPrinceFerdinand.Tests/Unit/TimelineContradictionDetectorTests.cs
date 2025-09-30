using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Contradictions.Detectors;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.Tests.Unit
{
    public class TimelineContradictionDetectorTests
    {
        private readonly Mock<ITestimonyQueryService> _mockTestimonyQuery;
        private readonly Mock<IInvestigationContext> _mockContext;
        private readonly TimelineContradictionsDetector _detector;

        public TimelineContradictionDetectorTests()
        {
            _mockTestimonyQuery = new Mock<ITestimonyQueryService>();
            _mockContext = new Mock<IInvestigationContext>();
            _detector = new TimelineContradictionsDetector(_mockTestimonyQuery.Object);
        }

        [Fact]
        public void HandledType_ShouldReturnTimeline()
        {
            Assert.Equal(ContradictionType.Timeline, _detector.HandledType);
        }

        [Fact]
        public void CanHandle_WithTimelineQuery_ShouldReturnTrue()
        {
            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Timeline);

            var result = _detector.CanHandle(query);

            Assert.True(result);
        }

        [Fact]
        public void CanHandle_WithNonTimelineQuery_ShouldReturnFalse()
        {
            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = _detector.CanHandle(query);

            Assert.False(result);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_TimeConflict_ShouldDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_assassin_002",
                SuspectId = "su_assassin_marko",
                OriginalText = "My train got in around 1 PM. I was late."
            };

            var evidence = new Evidence
            {
                Id = "ev_tickets_001",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "arrival_time", "11:50" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_assassin_002")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_tickets_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_assassin_002", "ev_tickets_001", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Equal(ContradictionType.Timeline, result.Type);
            Assert.Contains("su_assassin_marko", result.AffectedSuspects);
            Assert.Contains("ev_tickets_001", result.RelatedEvidence);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_TimeConsistent_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I arrived at approximately 11:50 AM."
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "arrival_time", "11:50" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_WithinTolerance_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I arrived around 11:55 AM."
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "arrival_time", "11:50" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
        }

        [Fact]
        public async Task DetectAsync_TestimonyNoTimeInfo_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at the cafe."
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "arrival_time", "11:50" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("No time information", result.Description);
        }

        [Fact]
        public async Task DetectAsync_EvidenceVsEvidence_TimeConflict_ShouldDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "arrival_time", "11:50" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "documents",
                Title = "Station Log",
                Content = new Dictionary<string, object>
                {
                    { "arrival_time", "13:00" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Equal(ContradictionType.Timeline, result.Type);
            Assert.Contains("ev_001", result.RelatedEvidence);
            Assert.Contains("ev_002", result.RelatedEvidence);
        }

        [Fact]
        public async Task DetectAsync_MissingTestimony_ShouldReturnNoContradiction()
        {
            _mockContext.Setup(c => c.GetTestimony("ts_missing")).Returns((TestimonyStatement?)null);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(new Evidence { Id = "ev_001" });

            var query = new TestimonyVsEvidenceQuery("ts_missing", "ev_001", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("not found", result.Description);
        }

        [Fact]
        public async Task DetectAsync_MissingEvidence_ShouldReturnNoContradiction()
        {
            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(new TestimonyStatement { Id = "ts_001" });
            _mockContext.Setup(c => c.GetEvidence("ev_missing")).Returns((Evidence?)null);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_missing", ContradictionType.Timeline);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("not found", result.Description);
        }
    }
}
