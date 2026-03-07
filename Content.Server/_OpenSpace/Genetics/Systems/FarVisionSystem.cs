using Content.Shared.Genetics;
using Content.Shared.Genetics.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._OpenSpace.Genetics.Systems;

public sealed class FarVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FarVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FarVisionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<FarVisionComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<EyeComponent>(ent, out var eye))
            return;

        
        _eye.SetDrawFov(ent, false, eye);
    }

    private void OnShutdown(Entity<FarVisionComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<EyeComponent>(ent, out var eye))
            return;

        
        _eye.SetDrawFov(ent, true, eye);
    }
}

