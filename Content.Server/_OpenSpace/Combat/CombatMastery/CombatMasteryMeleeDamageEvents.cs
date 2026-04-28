namespace Content.Server._OpenSpace.Combat.CombatMastery;

[ByRefEvent]
public sealed class CombatMasteryRefreshMeleeDamageEvent : EntityEventArgs;

[ByRefEvent]
public sealed class CombatMasteryCollectMeleeDamageEvent : EntityEventArgs
{
    public float HighestDamage { get; private set; }

    public CombatMasteryCollectMeleeDamageEvent(float initialDamage)
    {
        HighestDamage = initialDamage;
    }

    public void ConsiderDamage(float damage)
    {
        if (damage > HighestDamage)
            HighestDamage = damage;
    }
}
