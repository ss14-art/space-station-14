using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Shared._OpenSpace.CCVar;
using Content.Shared._OpenSpace.Discord;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._OpenSpace;

public sealed class DiscordOAuthManager : IDiscordOAuthManager, IDisposable
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _netMgr = default!;
    private readonly HttpClient _httpClient = new();

    private readonly ConcurrentDictionary<Guid, HashSet<ulong>> _playerRoles = [];

    public void Initialize()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.GetCVar(OpenSpaceCCvar.AuthApiToken));
        _netMgr.RegisterNetMessage<DiscordLinkRequestMessage>(OnLinkRequested);
        _netMgr.RegisterNetMessage<DiscordLinkResponseMessage>();
        _netMgr.RegisterNetMessage<DiscordRolesUpdateMessage>();
    }

    private async void OnLinkRequested(DiscordLinkRequestMessage msg)
    {
        var uuid = msg.MsgChannel.UserId.ToString();
        var link = await GetDiscordLink(uuid);
        var registered = await AlreadyRegistered(uuid);
        if (!string.IsNullOrEmpty(link))
        {
            var response = new DiscordLinkResponseMessage()
            {
                Link = link,
                AlreadyRegistered = registered,
            };
            _netMgr.ServerSendMessage(response, msg.MsgChannel);
        }
    }

    public async Task<HashSet<ulong>> GetRoles(ICommonSession session)
    {
        if (_cfg.GetCVar(OpenSpaceCCvar.AuthApiUrl) is not { } apiUrl || string.IsNullOrEmpty(apiUrl))
            return [];
        var url = new Uri(apiUrl);
        var guild = _cfg.GetCVar(OpenSpaceCCvar.AuthTargetGuild);
        var request = await _httpClient.GetAsync(new Uri(url, $"api/roles?method=uid&id={session.UserId.ToString()}&guildId={guild}").ToString());

        if (!request.IsSuccessStatusCode)
            return [];

        var response = await request.Content.ReadAsStringAsync();

        var json = JsonSerializer.Deserialize<DiscordRolesResponse>(response);

        HashSet<ulong> parsed = [];

        foreach (var role in json?.Roles ?? [])
        {
            if (ulong.TryParse(role, out var roleID))
                parsed.Add(roleID);
        }

        _netMgr.ServerSendMessage(new DiscordRolesUpdateMessage() { Roles = parsed }, session.Channel);
        _playerRoles[session.UserId] = parsed;

        return parsed;
    }

    public async Task<bool> AlreadyRegistered(string uuid)
    {
        if (_cfg.GetCVar(OpenSpaceCCvar.AuthApiUrl) is not { } apiUrl || string.IsNullOrEmpty(apiUrl))
            return false;

        var url = new Uri(apiUrl);
        var request = await _httpClient.GetAsync(new Uri(url, $"api/uuid?method=uid&id={uuid}").ToString());

        if (request.IsSuccessStatusCode)
            return true;

        return false;
    }

    public bool TryGetRoles(ICommonSession session, [NotNullWhen(true)] out HashSet<ulong>? roles)
        => _playerRoles.TryGetValue(session.UserId, out roles);

    public async Task<string?> GetDiscordLink(string uuid)
    {
        if (_cfg.GetCVar(OpenSpaceCCvar.AuthApiUrl) is not { } apiUrl || string.IsNullOrEmpty(apiUrl))
            return null;
        var url = new Uri(apiUrl);

        var request = await _httpClient.GetAsync(new Uri(url, $"api/link?uid={uuid}").ToString());

        if (request.IsSuccessStatusCode)
        {
            var response = await request.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<DiscordLinkResponse>(response);
            if (Uri.IsWellFormedUriString(json?.Link, UriKind.RelativeOrAbsolute))
                return json?.Link;
        }
        return null;
    }

    public void Dispose()
        => _httpClient.Dispose();
}

public sealed class DiscordLinkResponse
{
    [JsonPropertyName("link")]
    public string Link { get; set; } = "";
}

public sealed class DiscordRolesResponse
{
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];
}