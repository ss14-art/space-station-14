using Robust.Client.Graphics;

namespace Content.Client._OpenSpace.Effects;

[RegisterComponent]
public sealed partial class OutlineFlashEffectComponent : Component
{
    public ShaderInstance? AppliedShader;
    public TimeSpan EndTime;
}
