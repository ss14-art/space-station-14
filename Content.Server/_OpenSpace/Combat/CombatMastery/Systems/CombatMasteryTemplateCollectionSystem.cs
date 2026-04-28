using System;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Shared._OpenSpace.Combat.CombatMastery;

namespace Content.Server._OpenSpace.Combat.CombatMastery.Systems;

public abstract class CombatMasteryTemplateCollectionSystem<TComponent> : EntitySystem
    where TComponent : Component, ICombatMasteryTemplateProvider
{
    protected virtual string StyleId => typeof(TComponent).FullName ?? typeof(TComponent).Name;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComponent, CombatMasteryComboUpdatedEvent>(HandleComboUpdated);
        SubscribeLocalEvent<TComponent, CombatMasteryComboResetEvent>(OnComboReset);
        SubscribeLocalEvent<TComponent, CombatMasterySelectActiveStyleEvent>(OnSelectActiveStyle);
        SubscribeLocalEvent<TComponent, CombatMasteryActiveStyleChangedEvent>(OnActiveStyleChanged);
        SubscribeLocalEvent<TComponent, ComponentStartup>(OnMasteryStartup);
        SubscribeLocalEvent<TComponent, ComponentShutdown>(OnMasteryShutdown);
    }

    public bool TryAddOrReplaceTemplate(Entity<TComponent> ent, CombatMasteryTemplate template)
    {
        if (!TryAddOrReplaceTemplate(ent.Comp.TemplateCollection, template))
            return false;

        Dirty(ent);
        return true;
    }

    public bool TryRemoveTemplate(Entity<TComponent> ent, string templateName)
    {
        if (!TryRemoveTemplate(ent.Comp.TemplateCollection, templateName))
            return false;

        Dirty(ent);
        return true;
    }

    private void HandleComboUpdated(Entity<TComponent> ent, ref CombatMasteryComboUpdatedEvent args)
    {
        if (!IsMasteryActive(ent))
            return;

        OnComboUpdated(ent, ref args);

        if (args.TemplateExecuted)
            return;

        var target = args.Target;
        args.TemplateExecuted = TryProcessStep(
            ent.Comp.TemplateCollection,
            target,
            args.Step,
            template => OnTemplateMatched(ent, target, template));
    }

    private static void OnComboReset(Entity<TComponent> ent, ref CombatMasteryComboResetEvent args)
    {
        ResetProgress(ent.Comp.TemplateCollection);
    }

    private void OnSelectActiveStyle(Entity<TComponent> ent, ref CombatMasterySelectActiveStyleEvent args)
    {
        args.ConsiderStyle(StyleId, ent.Comp.StylePriority);
    }

    private void OnActiveStyleChanged(Entity<TComponent> ent, ref CombatMasteryActiveStyleChangedEvent args)
    {
        var styleId = StyleId;

        if (args.OldStyleId == styleId)
        {
            ResetProgress(ent.Comp.TemplateCollection);
            OnMasteryStopped(ent);
        }

        if (args.NewStyleId == styleId)
        {
            ResetProgress(ent.Comp.TemplateCollection);
            OnMasteryStarted(ent);
        }
    }

    private void OnMasteryStartup(Entity<TComponent> ent, ref ComponentStartup args)
    {
        ResetProgress(ent.Comp.TemplateCollection);
        RequestActiveStyleRefresh(ent.Owner);
    }

    private void OnMasteryShutdown(Entity<TComponent> ent, ref ComponentShutdown args)
    {
        ResetProgress(ent.Comp.TemplateCollection);
        RequestActiveStyleRefresh(ent.Owner, StyleId);
    }

    protected virtual void OnComboUpdated(Entity<TComponent> ent, ref CombatMasteryComboUpdatedEvent args)
    {
        if (args.TemplateExecuted)
            return;
    }

    protected virtual void OnMasteryStarted(Entity<TComponent> ent)
    {
    }

    protected virtual void OnMasteryStopped(Entity<TComponent> ent)
    {
    }

    protected abstract bool OnTemplateMatched(Entity<TComponent> ent, EntityUid target, CombatMasteryTemplate template);

    protected bool IsMasteryActive(Entity<TComponent> ent)
    {
        return TryComp<CombatMasteryComponent>(ent.Owner, out var mastery) &&
               mastery.ActiveStyleId == StyleId;
    }

    protected void RequestActiveStyleRefresh(EntityUid uid, string? ignoredStyleId = null)
    {
        var ev = new CombatMasteryRefreshActiveStyleEvent(ignoredStyleId);
        RaiseLocalEvent(uid, ref ev);
    }

    private static bool TryAddOrReplaceTemplate(CombatMasteryTemplateCollection collection, CombatMasteryTemplate template)
    {
        if (!template.IsValid())
            return false;

        var existingIndex = collection.Templates.FindIndex(x => x.Name == template.Name);
        if (existingIndex >= 0)
            collection.Templates[existingIndex] = template;
        else
            collection.Templates.Add(template);

        ResetProgress(collection);
        return true;
    }

    private static bool TryRemoveTemplate(CombatMasteryTemplateCollection collection, string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;

        var removed = collection.Templates.RemoveAll(x => x.Name == templateName) > 0;
        if (removed)
            ResetProgress(collection);

        return removed;
    }

    private static bool TryProcessStep(
        CombatMasteryTemplateCollection collection,
        EntityUid target,
        ComboMasteryKeys step,
        Func<CombatMasteryTemplate, bool> onTemplateMatched)
    {
        if (onTemplateMatched == null)
            return false;

        return TryProcessStepInternal(collection, target, step, onTemplateMatched, allowRestart: true);
    }

    private static bool TryProcessStepInternal(
        CombatMasteryTemplateCollection collection,
        EntityUid target,
        ComboMasteryKeys step,
        Func<CombatMasteryTemplate, bool> onTemplateMatched,
        bool allowRestart)
    {
        EnsureActiveTemplatesReady(collection);

        if (collection.ActiveTemplates.Count == 0)
            return false;

        for (var index = 0; index < collection.ActiveTemplates.Count;)
        {
            var state = collection.ActiveTemplates[index];
            if (!MatchesNextStep(state, target, step))
            {
                collection.ActiveTemplates.RemoveAt(index);
                continue;
            }

            state.CurrentTarget = target;
            state.NextStepIndex++;

            if (state.NextStepIndex < state.Template.Sequence.Count)
            {
                index++;
                continue;
            }

            if (onTemplateMatched(state.Template))
            {
                ResetProgress(collection);
                return true;
            }

            collection.ActiveTemplates.RemoveAt(index);
        }

        if (collection.ActiveTemplates.Count > 0 || !allowRestart)
            return false;

        ResetProgress(collection);
        return TryProcessStepInternal(collection, target, step, onTemplateMatched, allowRestart: false);
    }

    private static void EnsureActiveTemplatesReady(CombatMasteryTemplateCollection collection)
    {
        if (!collection.ActiveTemplatesDirty)
            return;

        collection.ActiveTemplates.Clear();
        foreach (var template in collection.Templates)
        {
            if (!template.IsValid())
                continue;

            collection.ActiveTemplates.Add(new CombatMasteryTemplateProgressState
            {
                Template = template,
            });
        }

        collection.ActiveTemplatesDirty = false;
    }

    private static bool MatchesNextStep(CombatMasteryTemplateProgressState state, EntityUid target, ComboMasteryKeys step)
    {
        if (state.NextStepIndex >= state.Template.Sequence.Count)
            return false;

        if (state.Template.RequireSameTarget &&
            state.CurrentTarget is { } currentTarget &&
            currentTarget != target)
        {
            return false;
        }

        return state.Template.Sequence[state.NextStepIndex] == step;
    }

    private static void ResetProgress(CombatMasteryTemplateCollection collection)
    {
        collection.ActiveTemplates.Clear();
        collection.ActiveTemplatesDirty = true;
    }
}
