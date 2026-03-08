using System.Threading.Tasks;

namespace Content.Server._OpenSpace;

public interface IDiscordOAuthManager
{
    Task<HashSet<ulong>> GetRoles(string uuid);
    Task<string?> GetDiscordLink(string uuid);
}