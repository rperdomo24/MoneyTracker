using Microsoft.Extensions.Options;
using MoneyTracker.UI.Utility.Settings;

namespace MoneyTracker.UI.Services.User
{
    public class CurrentUserKeyProvider : ICurrentUserKeyProvider
    {
        private readonly UISettings _settings;

        public CurrentUserKeyProvider(IOptions<UISettings> options)
        {
            _settings = options.Value;
        }

        public string GetUserKey()
            => string.IsNullOrWhiteSpace(_settings.UserKey) ? "dev-user" : _settings.UserKey;
    }
}
