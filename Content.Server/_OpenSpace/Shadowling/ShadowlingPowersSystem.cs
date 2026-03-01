using Content.Shared._OpenSpace.Shadowling;
using Content.Shared.CollectiveMind;

namespace Content.Server._OpenSpace.Shadowling;

public sealed class ShadowlingPowersSystem : EntitySystem
{
    [Dependency] private readonly SharedCollectiveMindSystem _collectiveMind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingPowersComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ShadowlingPowersComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, ShadowlingPowersComponent component, ComponentStartup args)
    {
        EnsureComp<ShadowlingComponent>(uid);
        EnsureComp<ShadowlingEnthrallComponent>(uid);
        EnsureComp<ShadowlingGlareComponent>(uid);
        EnsureComp<ShadowlingVeilComponent>(uid);
        EnsureComp<ShadowlingIcyVeinsComponent>(uid);
        EnsureComp<ShadowlingShadowWalkComponent>(uid);
        EnsureComp<ShadowlingSonicScreechComponent>(uid);
        EnsureComp<ShadowlingCollectiveMindComponent>(uid);

        if (TryComp<CollectiveMindComponent>(uid, out var collective))
            _collectiveMind.UpdateCollectiveMind(uid, collective);
    }

    private void OnShutdown(EntityUid uid, ShadowlingPowersComponent component, ComponentShutdown args)
    {
        RemComp<ShadowlingEnthrallComponent>(uid);
        RemComp<ShadowlingGlareComponent>(uid);
        RemComp<ShadowlingVeilComponent>(uid);
        RemComp<ShadowlingIcyVeinsComponent>(uid);
        RemComp<ShadowlingShadowWalkComponent>(uid);
        RemComp<ShadowlingSonicScreechComponent>(uid);
        RemComp<ShadowlingCollectiveMindComponent>(uid);

        if (TryComp<CollectiveMindComponent>(uid, out var collective))
            _collectiveMind.UpdateCollectiveMind(uid, collective);
    }
}
