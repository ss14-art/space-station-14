namespace Content.Client._OpenSpace;

public interface IClientDiscordOAuthManager
{
    void RequestLink();

    bool ContainsAny(ulong[] roles);

    void Initialize();
}