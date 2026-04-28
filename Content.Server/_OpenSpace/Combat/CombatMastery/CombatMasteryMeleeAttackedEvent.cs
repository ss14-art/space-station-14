using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._OpenSpace.Combat.CombatMastery;

[ByRefEvent]
public sealed class CombatMasteryMeleeAttackedEvent : EntityEventArgs
{
    public EntityUid Attacker { get; }
    public EntityUid Target { get; }
    public AttackedEvent Attack { get; }

    public CombatMasteryMeleeAttackedEvent(EntityUid attacker, EntityUid target, AttackedEvent attack)
    {
        Attacker = attacker;
        Target = target;
        Attack = attack;
    }
}
