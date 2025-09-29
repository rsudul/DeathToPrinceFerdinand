using Microsoft.Extensions.DependencyInjection;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Services;
using DeathToPrinceFerdinand.scripts.Infrastructure;

namespace DeathToPrinceFerdinand.scripts.Core.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddContradictionSystem(this IServiceCollection services, string dataPath = "Data")
        {
            services.AddSingleton<IInvestigationRepository>(provider =>
                new JsonInvestigationRepository(dataPath));

            services.AddScoped<IContradictionEventPublisher, ContradictionEventPublisher>();
            services.AddScoped<IInvestigationContext, InvestigationContext>();
            services.AddScoped<IContradictionService, ContradictionService>();

            services.AddScoped<ITestimonyQueryService, TestimonyQueryService>();
            services.AddScoped<IEvidenceQueryService, EvidenceQueryService>();

            services.AddScoped<IContradictionQueryFactory, ContradictionQueryFactory>();

            return services;
        }
    }
}
