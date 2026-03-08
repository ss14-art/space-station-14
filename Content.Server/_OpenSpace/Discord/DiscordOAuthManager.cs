using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared._OpenSpace.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._OpenSpace;

public sealed class DiscordOAuthManager : IDiscordOAuthManager, IDisposable
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly HttpClient _httpClient = new();

    public async Task<HashSet<ulong>> GetRoles(string uuid)
    {
        if (_cfg.GetCVar(OpenSpaceCCvar.AuthApiUrl) is not { } apiUrl || string.IsNullOrEmpty(apiUrl))
            return [];
        var url = new Uri(apiUrl);
        var guild = _cfg.GetCVar(OpenSpaceCCvar.AuthTargetGuild);
        var response = await _httpClient.GetAsync(new Uri(url, $"api/roles?method=uid&id={uuid}&guildId={guild}").ToString());

        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync();

        var roles = JsonSerializer.Deserialize<HashSet<ulong>>(json);

        return roles ?? [];
    }

    public async Task<string?> GetDiscordLink(string uuid)
    {
        if (_cfg.GetCVar(OpenSpaceCCvar.AuthApiUrl) is not { } apiUrl || string.IsNullOrEmpty(apiUrl))
            return null;
        var url = new Uri(apiUrl);

        var response = await _httpClient.GetAsync(new Uri(url, $"api/link?uid={uuid}").ToString());

        if (response.IsSuccessStatusCode)
        {
            var link = await response.Content.ReadAsStringAsync();
            if (Uri.IsWellFormedUriString(link, UriKind.RelativeOrAbsolute))
                return link;
        }
        return null;
    }

    public void Dispose()
        => _httpClient.Dispose();
}