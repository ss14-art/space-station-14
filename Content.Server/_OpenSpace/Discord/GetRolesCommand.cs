using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._OpenSpace.Discord;

[AdminCommand(AdminFlags.Admin)]
public sealed class GetRolesCommand : LocalizedCommands
{
    [Dependency] private readonly IDiscordOAuthManager _discordAuth = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "getdiscordroles";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString($"shell-need-exactly-one-argument"));
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString($"shell-target-player-does-not-exist"));
            return;
        }

        var roles = await _discordAuth.GetRoles(player);

        shell.WriteLine($"Founded roles: {roles.Count}");
        foreach (var role in roles)
            shell.WriteLine(role.ToString());
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _players),
                "<playerName>");
        }

        return CompletionResult.Empty;
    }
}