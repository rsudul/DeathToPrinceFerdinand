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
    public class IdentityContradictionDetectorTests
    {
        private readonly Mock<ITestimonyQueryService> _mockTestimonyQuery;
        private readonly Mock<IInvestigationContext> _mockContext;
        private readonly IdentityContradictionDetector _detector;

        public IdentityContradictionDetectorTests()
        {
            _mockTestimonyQuery = new Mock<ITestimonyQueryService>();
            _mockContext = new Mock<IInvestigationContext>();
            _detector = new IdentityContradictionDetector(_mockTestimonyQuery.Object);
        }

        [Fact]
        public void HandledType_ShouldReturnIdentity()
        {
            Assert.Equal(ContradictionType.Identity, _detector.HandledType);
        }

        [Fact]
        public void CanHandle_WithIdentityQuery_ShouldReturnTrue()
        {
            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Identity);

            var result = _detector.CanHandle(query);

            Assert.True(result);
        }

        [Fact]
        public void CanHandle_WithNonIdentityQuery_ShouldReturnFalse()
        {
            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Location);

            var result = _detector.CanHandle(query);

            Assert.False(result);
        }

        [Fact]
        public async Task DetectAsync_TestimonyVsEvidence_IdentityConflict_ShouldDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_assassin_marko",
                OriginalText = "That ticket isn't mine.",
                Metadata = new Dictionary<string, object>
                {
                    { "denied_identity", "N. Petrovic" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Work Permit",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "N. Petrovic" }
                }
            };

            var dossier = new DossierState
            {
                SuspectId = "su_assassin_marko",
                Name = "Marko Jovanović",
                Alias = "N. Petrovic"
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);
            _mockContext.Setup(c => c.GetDossier("su_assassin_marko")).Returns(dossier);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Equal(ContradictionType.Identity, result.Type);
            Assert.Contains("su_assassin_marko", result.AffectedSuspects);
            Assert.Contains("denies", result.Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DetectAsync_EvidenceVsEvidence_DifferentNames_ShouldDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_tickets_001",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "passenger_name", "N. Petrovic" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_documents_002",
                Category = "documents",
                Title = "Work Permit",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Marko Jovanovic" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_tickets_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_documents_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_tickets_001", "ev_documents_002", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Equal(ContradictionType.Identity, result.Type);
            Assert.Contains("ev_tickets_001", result.RelatedEvidence);
            Assert.Contains("ev_documents_002", result.RelatedEvidence);
            Assert.Contains("N. Petrovic", result.Description);
            Assert.Contains("Marko Jovanovic", result.Description);
        }

        [Fact]
        public async Task DetectAsync_EvidenceVsEvidence_SameNames_ShouldNotDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Ticket",
                Content = new Dictionary<string, object>
                {
                    { "passenger_name", "Marko Jovanovic" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "documents",
                Title = "Permit",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Marko Jovanovic" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
        }

        [Fact]
        public async Task DetectAsync_InitialsMatch_ShouldNotDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Ticket",
                Content = new Dictionary<string, object>
                {
                    { "passenger_name", "M. Petrovic" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "documents",
                Title = "Document",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Marko Petrovic" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction, "Initial matching same surname should not be a contradiction");
        }

        [Fact]
        public async Task DetectAsync_PartialNameMatch_ShouldNotDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Receipt",
                Content = new Dictionary<string, object>
                {
                    { "name", "Marko" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "documents",
                Title = "Permit",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Marko Jovanovic" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction, "Partial name match should not be a contradiction");
        }

        [Fact]
        public async Task DetectAsync_InitialsDontMatch_ShouldDetectContradiction()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "tickets",
                Title = "Ticket",
                Content = new Dictionary<string, object>
                {
                    { "passenger_name", "V. Petrovic" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "documents",
                Title = "Document",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Marko Petrovic" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction, "Different initials should be a contradiction");
        }

        [Fact]
        public async Task DetectAsync_TestimonyNoIdentityInfo_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "I was at the cafe.",
                Metadata = new Dictionary<string, object>
                {
                    { "topic", "alibi" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Permit",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Test Person" }
                }
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("No identity information", result.Description);
        }

        [Fact]
        public async Task DetectAsync_EvidenceNoIdentityInfo_ShouldNotDetectContradiction()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "That's not me.",
                Metadata = new Dictionary<string, object>
                {
                    { "denied_identity", "Viktor" }
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

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("No identity information", result.Description);
        }

        [Fact]
        public async Task DetectAsync_MissingTestimony_ShouldReturnNoContradiction()
        {
            _mockContext.Setup(c => c.GetTestimony("ts_missing")).Returns((TestimonyStatement?)null);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(new Evidence { Id = "ev_001" });

            var query = new TestimonyVsEvidenceQuery("ts_missing", "ev_001", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("not found", result.Description);
        }

        [Fact]
        public async Task DetectAsync_MissingEvidence_ShouldReturnNoContradiction()
        {
            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(new TestimonyStatement { Id = "ts_001" });
            _mockContext.Setup(c => c.GetEvidence("ev_missing")).Returns((Evidence?)null);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_missing", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.False(result.IsContradiction);
            Assert.Contains("not found", result.Description);
        }

        [Fact]
        public async Task DetectAsync_MultipleNameFields_ShouldCheckAll()
        {
            var evidence1 = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Document 1",
                Content = new Dictionary<string, object>
                {
                    { "passenger_name", "Viktor Drovenko" },
                    { "occupant_name", "Test Name" }
                }
            };

            var evidence2 = new Evidence
            {
                Id = "ev_002",
                Category = "documents",
                Title = "Document 2",
                Content = new Dictionary<string, object>
                {
                    { "full_name", "Marko Jovanovic" }
                }
            };

            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence1);
            _mockContext.Setup(c => c.GetEvidence("ev_002")).Returns(evidence2);

            var query = new EvidenceVsEvidenceQuery("ev_001", "ev_002", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction, "Should find contradiction when checking multiple name fields");
        }

        [Fact]
        public async Task DetectAsync_DenialIdentity_DescriptionShouldIndicateDenial()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_001",
                SuspectId = "su_test",
                OriginalText = "That's not me in the photo.",
                Metadata = new Dictionary<string, object>
                {
                    { "denied_identity", "N. Petrovic" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_001",
                Category = "documents",
                Title = "Document",
                Content = new Dictionary<string, object>
                {
                    { "name", "N. Petrovic" }
                }
            };

            var dossier = new DossierState
            {
                SuspectId = "su_test",
                Name = "Marko Jovanović",
                Alias = "N. Petrovic"
            };

            _mockContext.Setup(c => c.GetTestimony("ts_001")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_001")).Returns(evidence);
            _mockContext.Setup(c => c.GetDossier("su_test")).Returns(dossier);

            var query = new TestimonyVsEvidenceQuery("ts_001", "ev_001", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction);
            Assert.Contains("denies", result.Description.ToLower());
        }

        [Fact]
        public async Task DetectAsync_ShouldDetectContradiction_WhenSuspectDeniesNameThatMatchesEvidence()
        {
            var testimony = new TestimonyStatement
            {
                Id = "ts_denial",
                SuspectId = "su_assassin_marko",
                OriginalText = "That ticket isn't mine. Must've been planted.",
                Metadata = new Dictionary<string, object>
                {
                    { "denied_identity", "N. Petrovic" }
                }
            };

            var evidence = new Evidence
            {
                Id = "ev_ticket",
                Category = "tickets",
                Title = "Train Ticket",
                Content = new Dictionary<string, object>
                {
                    { "passenger_name", "N. Petrovic" }
                }
            };

            var dossier = new DossierState
            {
                SuspectId = "su_assassin_marko",
                Name = "Marko Jovanović",
                Alias = "N. Petrovic"
            };

            _mockContext.Setup(c => c.GetTestimony("ts_denial")).Returns(testimony);
            _mockContext.Setup(c => c.GetEvidence("ev_ticket")).Returns(evidence);
            _mockContext.Setup(c => c.GetDossier("su_assassin_marko")).Returns(dossier);

            var query = new TestimonyVsEvidenceQuery("ts_denial", "ev_ticket", ContradictionType.Identity);

            var result = await _detector.DetectAsync(query, _mockContext.Object);

            Assert.True(result.IsContradiction, "Denying a name that matches evidence should be a contradiction");
            Assert.Equal(ContradictionType.Identity, result.Type);
            Assert.Contains("denies", result.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("N. Petrovic", result.Description);
        }
    }
}
