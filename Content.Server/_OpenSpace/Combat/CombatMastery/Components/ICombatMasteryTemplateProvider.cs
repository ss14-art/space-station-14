namespace Content.Server._OpenSpace.Combat.CombatMastery.Components;

public interface ICombatMasteryTemplateProvider
{
    CombatMasteryTemplateCollection TemplateCollection { get; }
    int StylePriority { get; }
}
