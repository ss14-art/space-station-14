using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Player;

namespace Content.Server._OpenSpace;

public interface IDiscordOAuthManager
{
    Task<HashSet<ulong>> GetRoles(ICommonSession session);
    Task<string?> GetDiscordLink(string uuid);

    bool TryGetRoles(ICommonSession session, [NotNullWhen(true)] out HashSet<ulong>? roles);

    void Initialize();
}