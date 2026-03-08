namespace Content.Client._OpenSpace;

public interface IClientDiscordOAuthManager
{
    event Action<string>? LinkReceived;

    void RequestLink();

    void Initialize();
}