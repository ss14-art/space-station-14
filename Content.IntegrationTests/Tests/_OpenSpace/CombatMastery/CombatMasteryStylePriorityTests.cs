using System.Numerics;
using Content.Server._OpenSpace.Combat.CloseQuarterCombatMastery.Components;
using Content.Server._OpenSpace.Combat.CloseQuarterCooking.Components;
using Content.Server._OpenSpace.Combat.CombatMastery.Components;
using Content.Server._OpenSpace.Combat.CorporateJudo.Components;
using Content.Server._OpenSpace.Combat.MimeJutsu.Components;
using Content.Server._OpenSpace.Combat.SleepingCarp.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Reflect;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._OpenSpace.CombatMastery;

[TestFixture]
public sealed class CombatMasteryStylePriorityTests
{
    [Test]
    public async Task ScrollSequenceAndJudoBelt_ActiveStyleAndBonusesRemainConsistent()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        var entMan = server.ResolveDependency<IEntityManager>();
        var hands = entMan.System<SharedHandsSystem>();
        var interaction = entMan.System<SharedInteractionSystem>();
        var inventory = entMan.System<InventorySystem>();

        EntityUid user = default;
        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertNoActiveStyle(entMan, user);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(5f).Within(0.001f));
        });

        EntityUid cqcScroll = default;
        await server.WaitAssertion(() =>
        {
            cqcScroll = entMan.SpawnEntity("CqcManualScroll", map.GridCoords);
            ReadScroll(entMan, hands, interaction, user, cqcScroll);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertActiveStyle<CloseQuarterCombatMasteryComponent>(entMan, user, 6);
            Assert.That(entMan.HasComponent<CloseQuarterCombatMasteryComponent>(user), Is.True);
            Assert.That(entMan.EntityExists(cqcScroll), Is.False);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        EntityUid carpScroll = default;
        await server.WaitAssertion(() =>
        {
            carpScroll = entMan.SpawnEntity("SleepingCarpScroll", map.GridCoords);
            ReadScroll(entMan, hands, interaction, user, carpScroll);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertActiveStyle<SleepingCarpMasteryComponent>(entMan, user, 9);
            Assert.That(entMan.EntityExists(carpScroll), Is.False);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(10f).Within(0.001f));

            Assert.That(entMan.TryGetComponent<ReflectComponent>(user, out var reflect), Is.True);
            Assert.That(reflect!.ShowExamineInfo, Is.False);
            Assert.That(reflect.ReflectProb, Is.EqualTo(1f).Within(0.001f));
        });

        EntityUid mimeScroll = default;
        await server.WaitAssertion(() =>
        {
            mimeScroll = entMan.SpawnEntity("MimeJutsuScroll", map.GridCoords);
            ReadScroll(entMan, hands, interaction, user, mimeScroll);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertActiveStyle<SleepingCarpMasteryComponent>(entMan, user, 9);
            Assert.That(entMan.HasComponent<MimeJutsuMasteryComponent>(user), Is.True);
            Assert.That(entMan.EntityExists(mimeScroll), Is.False);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(10f).Within(0.001f));
        });

        EntityUid judoBelt = default;
        await server.WaitAssertion(() =>
        {
            judoBelt = entMan.SpawnEntity("ClothingBeltCorporateJudo", map.GridCoords);
            Assert.That(inventory.TryEquip(user, judoBelt, "belt"), Is.True);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<CorporateJudoMasteryComponent>(user), Is.True);
            AssertActiveStyle<SleepingCarpMasteryComponent>(entMan, user, 9);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(10f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(inventory.TryUnequip(user, "belt"), Is.True);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<CorporateJudoMasteryComponent>(user), Is.False);
            AssertActiveStyle<SleepingCarpMasteryComponent>(entMan, user, 9);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(10f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            entMan.RemoveComponent<MimeJutsuMasteryComponent>(user);
            entMan.RemoveComponent<SleepingCarpMasteryComponent>(user);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<ReflectComponent>(user), Is.False);
            AssertActiveStyle<CloseQuarterCombatMasteryComponent>(entMan, user, 6);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            entMan.RemoveComponent<CloseQuarterCombatMasteryComponent>(user);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertNoActiveStyle(entMan, user);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(5f).Within(0.001f));
        });

        await server.WaitPost(() => server.System<SharedMapSystem>().DeleteMap(map.MapId));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CloseQuarterCooking_GateActivatesAndDisablesByTile()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSystem = server.System<SharedMapSystem>();
        var xform = entMan.System<TransformSystem>();
        var tileDefs = server.ResolveDependency<ITileDefinitionManager>();

        var allowedCoords = new EntityCoordinates(map.Grid, new Vector2(0.5f, 0.5f));
        var blockedCoords = new EntityCoordinates(map.Grid, new Vector2(1.5f, 0.5f));

        EntityUid user = default;
        await server.WaitAssertion(() =>
        {
            var grid = entMan.GetComponent<MapGridComponent>(map.Grid);
            SetGridTile(mapSystem, tileDefs, map.Grid, grid, Vector2i.Zero, "FloorKitchen");
            SetGridTile(mapSystem, tileDefs, map.Grid, grid, new Vector2i(1, 0), "Plating");

            user = entMan.SpawnEntity("MobHuman", allowedCoords);
            entMan.EnsureComponent<CloseQuarterCookingMasteryComponent>(user);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent<CloseQuarterCookingMasteryComponent>(user, out var cooking), Is.True);
            Assert.That(cooking!.CurrentPriority, Is.EqualTo(5));
            AssertActiveStyle<CloseQuarterCookingMasteryComponent>(entMan, user, 5);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            xform.SetCoordinates(user, blockedCoords);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent<CloseQuarterCookingMasteryComponent>(user, out var cooking), Is.True);
            Assert.That(cooking!.CurrentPriority, Is.EqualTo(0));
            AssertNoActiveStyle(entMan, user);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(5f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            xform.SetCoordinates(user, allowedCoords);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent<CloseQuarterCookingMasteryComponent>(user, out var cooking), Is.True);
            Assert.That(cooking!.CurrentPriority, Is.EqualTo(5));
            AssertActiveStyle<CloseQuarterCookingMasteryComponent>(entMan, user, 5);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitPost(() => mapSystem.DeleteMap(map.MapId));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CloseQuarterCooking_WithLearnedCqc_KeepsCqcActiveAcrossGateTransitions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSystem = server.System<SharedMapSystem>();
        var xform = entMan.System<TransformSystem>();
        var tileDefs = server.ResolveDependency<ITileDefinitionManager>();
        var hands = entMan.System<SharedHandsSystem>();
        var interaction = entMan.System<SharedInteractionSystem>();

        var allowedCoords = new EntityCoordinates(map.Grid, new Vector2(0.5f, 0.5f));
        var blockedCoords = new EntityCoordinates(map.Grid, new Vector2(1.5f, 0.5f));

        EntityUid user = default;
        await server.WaitAssertion(() =>
        {
            var grid = entMan.GetComponent<MapGridComponent>(map.Grid);
            SetGridTile(mapSystem, tileDefs, map.Grid, grid, Vector2i.Zero, "FloorKitchen");
            SetGridTile(mapSystem, tileDefs, map.Grid, grid, new Vector2i(1, 0), "Plating");

            user = entMan.SpawnEntity("MobHuman", allowedCoords);
            entMan.EnsureComponent<CloseQuarterCookingMasteryComponent>(user);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertActiveStyle<CloseQuarterCookingMasteryComponent>(entMan, user, 5);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            var cqcScroll = entMan.SpawnEntity("CqcManualScroll", allowedCoords);
            ReadScroll(entMan, hands, interaction, user, cqcScroll);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            AssertActiveStyle<CloseQuarterCombatMasteryComponent>(entMan, user, 6);
            Assert.That(entMan.TryGetComponent<CloseQuarterCookingMasteryComponent>(user, out var cooking), Is.True);
            Assert.That(cooking!.CurrentPriority, Is.EqualTo(5));
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            xform.SetCoordinates(user, blockedCoords);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent<CloseQuarterCookingMasteryComponent>(user, out var cooking), Is.True);
            Assert.That(cooking!.CurrentPriority, Is.EqualTo(0));
            AssertActiveStyle<CloseQuarterCombatMasteryComponent>(entMan, user, 6);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitAssertion(() =>
        {
            xform.SetCoordinates(user, allowedCoords);
        });
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent<CloseQuarterCookingMasteryComponent>(user, out var cooking), Is.True);
            Assert.That(cooking!.CurrentPriority, Is.EqualTo(5));
            AssertActiveStyle<CloseQuarterCombatMasteryComponent>(entMan, user, 6);
            Assert.That(GetUnarmedDamage(entMan, user), Is.EqualTo(13f).Within(0.001f));
        });

        await server.WaitPost(() => mapSystem.DeleteMap(map.MapId));
        await pair.CleanReturnAsync();
    }

    private static void ReadScroll(
        IEntityManager entMan,
        SharedHandsSystem hands,
        SharedInteractionSystem interaction,
        EntityUid user,
        EntityUid scroll)
    {
        var handsComp = entMan.GetComponent<HandsComponent>(user);
        Assert.That(handsComp.ActiveHandId, Is.Not.Null);
        Assert.That(hands.TryPickup(user, scroll, handsComp.ActiveHandId!), Is.True);
        Assert.That(interaction.UseInHandInteraction(user, scroll), Is.True);
    }

    private static float GetUnarmedDamage(IEntityManager entMan, EntityUid user)
    {
        var melee = entMan.GetComponent<MeleeWeaponComponent>(user);
        return melee.Damage.GetTotal().Float();
    }

    private static void AssertActiveStyle<TStyle>(IEntityManager entMan, EntityUid user, int expectedPriority)
        where TStyle : IComponent
    {
        var mastery = entMan.GetComponent<CombatMasteryComponent>(user);
        Assert.That(mastery.ActiveStyleId, Is.EqualTo(StyleId<TStyle>()));
        Assert.That(mastery.ActiveStylePriority, Is.EqualTo(expectedPriority));
    }

    private static void AssertNoActiveStyle(IEntityManager entMan, EntityUid user)
    {
        var mastery = entMan.GetComponent<CombatMasteryComponent>(user);
        Assert.That(mastery.ActiveStyleId, Is.Null);
        Assert.That(mastery.ActiveStylePriority, Is.EqualTo(0));
    }

    private static string StyleId<TStyle>() where TStyle : IComponent
    {
        return typeof(TStyle).FullName!;
    }

    private static void SetGridTile(
        SharedMapSystem mapSystem,
        ITileDefinitionManager tileDefs,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i indices,
        string tileId)
    {
        mapSystem.SetTile(gridUid, grid, indices, new Tile(tileDefs[tileId].TileId));
    }
}
