using System;
using Microsoft.Extensions.DependencyInjection;
using DeathToPrinceFerdinand.scripts.Core.DependencyInjection;

namespace DeathToPrinceFerdinand.scripts.Infrastructure
{
    public static class GameServices
    {
        private static IServiceProvider _serviceProvider;
        private static bool _isInitialized = false;

        public static bool IsInitialized => _isInitialized;

        public static void Initialize(string dataPath = "Data")
        {
            if (_isInitialized)
            {
                return;
            }

            var services = new ServiceCollection();
            services.AddContradictionSystem(dataPath);

            _serviceProvider = services.BuildServiceProvider();
            _isInitialized = true;
        }

        public static T GetService<T>() where T : class
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("GameServices not initialized. Call Initialize() first.");
            }

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
