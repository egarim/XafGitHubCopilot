using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace XafGitHubCopilot.Module.Services
{
    public sealed class CopilotChatService : IAsyncDisposable
    {
        private readonly CopilotClient _client;
        private readonly CopilotOptions _options;
        private readonly ILogger<CopilotChatService> _logger;
        private readonly SemaphoreSlim _startLock = new(1, 1);
        private bool _started;

        public CopilotChatService(IOptions<CopilotOptions> optionsAccessor, ILogger<CopilotChatService> logger)
        {
            _options = optionsAccessor?.Value ?? new CopilotOptions();
            _logger = logger;
            _client = new CopilotClient(new CopilotClientOptions
            {
                CliPath = string.IsNullOrWhiteSpace(_options.CliPath) ? null : _options.CliPath,
                GithubToken = string.IsNullOrWhiteSpace(_options.GithubToken) ? null : _options.GithubToken,
                UseLoggedInUser = string.IsNullOrWhiteSpace(_options.GithubToken) && _options.UseLoggedInUser,
                Logger = logger
            });
        }

        private async Task EnsureStartedAsync()
        {
            if (_started)
            {
                return;
            }

            await _startLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_started)
                {
                    return;
                }

                await _client.StartAsync().ConfigureAwait(false);
                _started = true;
            }
            finally
            {
                _startLock.Release();
            }
        }

        public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            await EnsureStartedAsync().ConfigureAwait(false);

            await using var session = await _client.CreateSessionAsync(new SessionConfig
            {
                Model = _options.Model,
                Streaming = _options.Streaming
            }).ConfigureAwait(false);

            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var buffer = new StringBuilder();

            var subscription = session.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent delta:
                        buffer.Append(delta.Data.DeltaContent);
                        break;
                    case AssistantMessageEvent message:
                        buffer.Append(message.Data.Content);
                        break;
                    case SessionErrorEvent error:
                        completion.TrySetException(new InvalidOperationException(error.Data.Message));
                        break;
                    case SessionIdleEvent:
                        completion.TrySetResult(buffer.ToString());
                        break;
                }
            });

            try
            {
                await session.SendAsync(new MessageOptions
                {
                    Prompt = prompt
                }).ConfigureAwait(false);

                return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                subscription.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_started)
            {
                try
                {
                    await _client.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stop Copilot client cleanly.");
                }
            }

            await _client.DisposeAsync().ConfigureAwait(false);
            _startLock.Dispose();
        }
    }
}
