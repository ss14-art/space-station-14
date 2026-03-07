using Content.Shared.Genetics.Components;
using Content.Shared.Genetics;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.Genetics.Systems
{
    public sealed class GeneticsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GeneticsConsoleComponent, ActivatableUIOpenAttemptEvent>(OnGeneticsConsoleOpenAttempt);
            SubscribeLocalEvent<GeneModificationConsoleComponent, ActivatableUIOpenAttemptEvent>(OnGeneModificationConsoleOpenAttempt);
        }

        private void OnGeneticsConsoleOpenAttempt(EntityUid uid, GeneticsConsoleComponent component, ActivatableUIOpenAttemptEvent args)
        {
            
        }

        private void OnGeneModificationConsoleOpenAttempt(EntityUid uid, GeneModificationConsoleComponent component, ActivatableUIOpenAttemptEvent args)
        {
            
        }
    }
}

