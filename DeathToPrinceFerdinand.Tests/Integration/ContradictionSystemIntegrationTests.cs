using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Contradictions.Detectors;
using DeathToPrinceFerdinand.scripts.Core.DependencyInjection;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.Tests.Integration
{
    public class ContradictionSystemIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _testDataPath;

        public ContradictionSystemIntegrationTests()
        {
            _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

            if (!Directory.Exists(_testDataPath))
            {
                Directory.CreateDirectory(_testDataPath);
            }

            var services = new ServiceCollection();
            services.AddContradictionSystem(_testDataPath);
            services.AddScoped<IContradictionDetector, TimelineContradictionsDetector>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task LoadJsonData_ShouldLoadAllFiles()
        {
            var repository = _serviceProvider.GetRequiredService<IInvestigationRepository>();

            var evidence = await repository.GetAllEvidenceAsync();
            var testimony = await repository.GetAllTestimonyAsync();
            var dossiers = await repository.GetAllDossiersAsync();

            Assert.NotEmpty(evidence);
            Assert.NotEmpty(testimony);
            Assert.NotEmpty(dossiers);
        }

        [Fact]
        public async Task LoadEvidence_TrainTicket_ShouldHaveCorrectData()
        {
            var repository = _serviceProvider.GetRequiredService<IInvestigationRepository>();

            var trainTicket = await repository.GetEvidenceAsync("ev_tickets_001");

            Assert.NotNull(trainTicket);
            Assert.Equal("ev_tickets_001", trainTicket.Id);
            Assert.Equal("tickets", trainTicket.Category);
            Assert.Equal("N. Petrovic", trainTicket.GetContentValue("passenger_name"));
            Assert.Equal("11:50", trainTicket.GetContentValue("arrival_time"));
        }

        [Fact]
        public async Task LoadTestimony_AssassinStatement_ShouldHaveCorrectData()
        {
            var repository = _serviceProvider.GetRequiredService<IInvestigationRepository>();

            var testimony = await repository.GetTestimonyAsync("ts_assassin_002");

            Assert.NotNull(testimony);
            Assert.Equal("ts_assassin_002", testimony.Id);
            Assert.Equal("su_assassin_marko", testimony.SuspectId);
            Assert.Contains("1 PM", testimony.OriginalText);
        }

        [Fact]
        public async Task LoadDossier_Assassin_ShouldHaveCorrectStructure()
        {
            var repository = _serviceProvider.GetRequiredService<IInvestigationRepository>();

            var dossier = await repository.GetDossierAsync("su_assassin_marko");

            Assert.NotNull(dossier);
            Assert.Equal("su_assassin_marko", dossier.SuspectId);
            Assert.Equal("Marko Jovanović", dossier.Name);
            Assert.Equal("N. Petrovic", dossier.Alias);
            Assert.Equal("The Assassin", dossier.Codename);
            Assert.NotEmpty(dossier.Testimony);
            Assert.NotEmpty(dossier.LinkedEvidenceIds);
        }

        [Fact]
        public async Task DetectContradiction_AssassinTrainTime_ShouldFindContradiction()
        {
            var service = _serviceProvider.GetRequiredService<IContradictionService>();
            var factory = _serviceProvider.GetRequiredService<IContradictionQueryFactory>();

            var query = factory.CreateTestimonyVsEvidence("ts_assassin_002", "ev_tickets_001", ContradictionType.Timeline);

            var result = await service.CheckContradictionAsync(query);

            Assert.True(result.IsContradiction, "Should detect contradiction between '1 PM' testimony and '11:50' ticket");
            Assert.Equal(ContradictionType.Timeline, result.Type);
            Assert.Contains("su_assassin_marko", result.AffectedSuspects);
            Assert.Contains("ev_tickets_001", result.RelatedEvidence);
        }

        [Fact]
        public async Task DetectContradiction_AssassinCafeTime_ShouldFindContradiction()
        {
            var service = _serviceProvider.GetRequiredService<IContradictionService>();
            var factory = _serviceProvider.GetRequiredService<IContradictionQueryFactory>();

            var query = factory.CreateTestimonyVsEvidence("ts_assassin_004", "ev_documents_001", ContradictionType.Timeline);

            var result = await service.CheckContradictionAsync(query);

            Assert.True(result.IsContradiction, "Should detect contradiction between '12:45' testimony and '12:15' receipt");
            Assert.Equal(ContradictionType.Timeline, result.Type);
        }

        [Fact]
        public async Task DetectContradiction_AssassinCafeAlibi_ShouldFindContradiction()
        {
            var service = _serviceProvider.GetRequiredService<IContradictionService>();
            var factory = _serviceProvider.GetRequiredService<IContradictionQueryFactory>();

            var query = factory.CreateTestimonyVsEvidence("ts_assassin_001", "ev_photos_001", ContradictionType.Timeline);

            var result = await service.CheckContradictionAsync(query);

            Assert.True(result.IsContradiction, "Should detect contradiction between cafe alibi and North Gate photo");
            Assert.Equal(ContradictionType.Timeline, result.Type);
        }

        [Fact]
        public async Task QueryService_FindBySuspect_ShouldReturnAssassinTestimony()
        {
            var queryService = _serviceProvider.GetRequiredService<ITestimonyQueryService>();

            var testimony = queryService.FindBySuspect("su_assassin_marko");

            Assert.NotEmpty(testimony);
            Assert.All(testimony, t => Assert.Equal("su_assassin_marko", t.SuspectId));
        }

        [Fact]
        public async Task QueryService_FindByCategory_ShouldReturnTickets()
        {
            var queryService = _serviceProvider.GetRequiredService<IEvidenceQueryService>();

            var tickets = queryService.FindByCategory("tickets");

            Assert.NotEmpty(tickets);
            Assert.All(tickets, e => Assert.Equal("tickets", e.Category));
        }

        [Fact]
        public async Task InvestigationContext_GetTestimony_ShouldWork()
        {
            var context = _serviceProvider.GetRequiredService<IInvestigationContext>();

            var testimony = context.GetTestimony("ts_assassin_001");

            Assert.NotNull(testimony);
            Assert.Equal("ts_assassin_01", testimony.Id);
        }

        [Fact]
        public async Task InvestigationContext_GetEvidence_ShouldWork()
        {
            var context = _serviceProvider.GetRequiredService<IInvestigationContext>();

            var evidence = context.GetEvidence("ev_tickets_001");

            Assert.NotNull(evidence);
            Assert.Equal("ev_tickets_001", evidence.Id);
        }

        [Fact]
        public async Task InvestigationContext_GetDossier_ShouldWork()
        {
            var context = _serviceProvider.GetRequiredService<IInvestigationContext>();

            var dossier = context.GetDossier("su_assassin_marko");

            Assert.NotNull(dossier);
            Assert.Equal("su_assassin_marko", dossier.SuspectId);
        }

        [Fact]
        public async Task FullWorkflow_DetectAndApplyResolution_ShouldUpdateDossier()
        {
            var service = _serviceProvider.GetRequiredService<IContradictionService>();
            var factory = _serviceProvider.GetRequiredService<IContradictionQueryFactory>();
            var context = _serviceProvider.GetRequiredService<IInvestigationContext>();

            var query = factory.CreateTestimonyVsEvidence("ts_assassin_002", "ev_tickets_001", ContradictionType.Timeline);

            var contradiction = await service.CheckContradictionAsync(query);
            Assert.True(contradiction.IsContradiction);

            contradiction.Resolution.AmendedTestimony = "Fine. I arrived at 11:50. Went straight to the cafe.";

            var resolvedContradiction = await service.ApplyResolutionAsync(contradiction);

            Assert.NotNull(resolvedContradiction);
            var isResolved = await service.IsContradictionResolvedAsync(contradiction.ContradictionId);
            Assert.True(isResolved);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
