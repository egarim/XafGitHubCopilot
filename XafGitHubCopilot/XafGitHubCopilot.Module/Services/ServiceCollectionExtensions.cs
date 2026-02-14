using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace XafGitHubCopilot.Module.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCopilotSdk(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<CopilotOptions>(configuration.GetSection(CopilotOptions.SectionName));
            services.AddSingleton<CopilotChatService>();

            // Register the IChatClient adapter so DevExpress DxAIChat / AIChatControl
            // can route messages through the GitHub Copilot SDK automatically.
            services.AddChatClient(sp => new CopilotChatClient(
                sp.GetRequiredService<CopilotChatService>()));

            return services;
        }
    }
}
