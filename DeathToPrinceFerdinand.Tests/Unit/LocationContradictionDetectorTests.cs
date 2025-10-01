using Moq;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Contradictions.Detectors;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.Tests.Unit
{
    public class LocationContradictionDetectorTests
    {
        private readonly Mock<ITestimonyQueryService> _mockTestimonyQuery;
        private readonly Mock<IInvestigationContext> _mockContext;
        private readonly LocationContradictionDetector _detector;

        public LocationContradictionDetectorTests()
        {
            _mockTestimonyQuery = new Mock<ITestimonyQueryService>();
            _mockContext = new Mock<IInvestigationContext>();
            _detector = new LocationContradictionDetector(_mockTestimonyQuery.Object);
        }

        [Fact]
        public void HandledType_ShouldReturnLocation()
        {
            Assert.Equal(ContradictionType.Location, _detector.HandledType);
        }

        [Fact]
        public void CanHandle_WithLocationQuery_ShouldReturnTrue()
        {
            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = _detector.CanHandle(query);

            Assert.True(result);
        }

        [Fact]
        public void CanHandle_WithNonLocationQuery_ShouldReturnFalse()
        {
            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Timeline);

            var result = _detector.CanHandle(query);

            Assert.False(result);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_LocationConflict_ShouldDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_assassin_001",
                SuspectId = "su_assassin_marko",
                OriginalText = "From noon until one I was at Cafe Lenestra, alone.",
                Metadata = new Dictionary<string, object>
                {
                    { "claimed_location", "Cafe Lenestra" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_photos_001",
                Category = "photos",
                Title = "Surveillance Photo - North Gate",
                Content = new Dictionary<string, object>
                {
                    { "location", "North Gate" },
                    { "time", "12:05" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_assassin_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_photos_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_assassin_001", "ev_photos_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Equal(ContradictionType.Location, result.Type);
            Assert.Contains("su_assassin_marko", result.AffectedSuspects);
            Assert.Contains("ev_photos_001", result.RelatedEvidence);
            Assert.Contains("Cafe Lenestra", result.Description);
            Assert.Contains("North Gate", result.Description);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_LocationConsistent_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at North Gate.",
                Metadata = new Dictionary<string, object>
                {
                    { "claimed_location", "North Gate" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "photos",
                Title = "Surveillance Photo",
                Content = new Dictionary<string, object>
                {
                    { "location", "North Gate" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_PartialLocationMatch_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at Cafe Lenestra",
                Metadata = new Dictionary<string, object>
                {
                    { "claimed_location", "Cafe Lenestra" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Receipt",
                Content = new Dictionary<string, object>
                {
                    { "location", "Lenestra" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction, "Partial location match should not be a contradiction");
        }

        [Fact]
        public async Task DetectAsync_TestimonyNoLocationInfo_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was there at noon.",
                Metadata = new Dictionary<string, object>
                {
                    { "topic", "alibi" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "photos",
                Title = "Photo",
                Content = new Dictionary<string, object>
                {
                    { "location", "North Gate" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("No location information", result.Description);
        }

        [Fact]
        public async Task DetectAsync_EvidenceNoLocationInfo_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at the cafe.",
                Metadata = new Dictionary<string, object>
                {
                    { "claimed_location", "Cafe Lenestra" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Document",
                Content = new Dictionary<string, object>
                {
                    { "time", "12:00" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("No location information", result.Description);
        }

        [Fact]
        public async Task DetectAsync_EvidenceVsEvidence_LocationConflict_ShouldDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Receipt",
                Content = new Dictionary<string, object>
                {
                    { "location", "Cafe Lenestra" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "photos",
                Title = "Photo",
                Content = new Dictionary<string, object>
                {
                    { "location", "North Gate" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Equal(ContradictionType.Location, result.Type);
            Assert.Contains("ev_001", result.RelatedEvidence);
            Assert.Contains("ev_002", result.RelatedEvidence);
        }

        [Fact]
        public async Task DetectAsync_MultipleLocationFields_ShouldCheckAll()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at the station.",
                Metadata = new Dictionary<string, object>
                {
                    { "claimed_location", "Dravik Station" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Travel Log",
                Content = new Dictionary<string, object>
                {
                    { "departure", "Varnograd Station" },
                    { "destination", "North Gate" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
        }

        [Fact]
        public async Task DetectAsync_MissingTestimony_ShouldReturnNoContradiction()
        {
            _mockContext.Setup(c => c.GetTestimony("ts_missing")).Returns((TestimonyStatement?)null);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(new Evidence { Id = "ev_001" });

            var query = new TestimonyVsEvidenceQuery("ts_missing", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("not found", result.Description);
        }

        [Fact]
        public async Task DetectAsync_MissingEvidence_ShouldReturnNoContradiction()
        {
            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(new TestimonyStatement { Id = "ts_001" });
            _mockContext.Setup(c => c.GetEvidence("ev_missing")).Returns((Evidence?)null);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_missing", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("not found", result.Description);
        }

        [Fact]
        public async Task DetectAsync_NormalizedLocations_ShouldMatchCorrectly()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at the cafe.",
                Metadata = new Dictionary<string, object>
                {
                    { "claimed_location", "Cafe Lenestra" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Receipt",
                Content = new Dictionary<string, object>
                {
                    { "location", "Lenestra Cafe" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction, "Normalized locations should match");
        }
    }
}