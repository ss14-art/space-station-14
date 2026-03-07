using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Genetics.Telekinesis;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelekinesisVisualComponent : Component
{
    public const string LayerKey = "TelekinesisHead";
    public const string RsiPath = "/Textures/_OpenSpace/Misc/Telekinesis.rsi";
    public const string State = "telekinesishead";
    public static readonly Vector2 Offset = new(0f, 0f);
}

