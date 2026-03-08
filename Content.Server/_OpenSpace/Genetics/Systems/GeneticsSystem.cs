using Content.Shared._OpenSpace.Genetics.Components;
using Content.Shared._OpenSpace.Genetics;
using Content.Shared.Mobs.Components; 
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;
using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking.Events;

using Content.Server.Medical.Components;
using Content.Server.Polymorph.Components;
using Robust.Server.GameObjects;
using Content.Shared.MedicalScanner;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Systems;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.GameTicking;
using Robust.Shared.Enums;
using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Labels.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Jittering;
using Content.Shared.Throwing;
using Content.Shared.Sprite;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Damage;
using Content.Shared._OpenSpace.Genetics.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Speech;
using Content.Shared.Strip.Components;
using Robust.Shared.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Item;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Forensics.Components;
using Content.Server.Body.Components;
using Content.Server.Atmos.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using System.Collections.Generic;
using System;
using System.Numerics;
using Robust.Shared.Localization;
using Content.Server.Speech.Components;
using System.Text.RegularExpressions;

namespace Content.Server._OpenSpace.Genetics.Systems
{
    public sealed class GeneticsSystem : EntitySystem
    {
        private static readonly HashSet<string> HulkAllowedActionPrototypes = new()
        {
            "ActionCombatModeToggle",
            "ActionCombatModeToggleOff",
            "ActionScream"
        };
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
        [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!; 
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _serverHumanoidAppearance = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedScaleVisualsSystem _scaleVisuals = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly MarkingManager _markingManager = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
        [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
        [Dependency] private readonly SharedElectrocutionSystem _electrocutionSystem = default!;

        private Dictionary<int, string> _geneMap = new();
        private Dictionary<string, string> _geneTargets = new();
        private Dictionary<string, int> _geneMinThresholds = new();
        private Dictionary<string, string> _geneMinThresholdHex = new();

        private const int UiBlockCount = 39;
        private const int SeBlockCount = 55;
        private const int SeHumanoidBlockIndex = SeBlockCount - 1;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GeneticsComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GeneticsConsoleComponent, MapInitEvent>(OnConsoleMapInit);
            SubscribeLocalEvent<GeneticsConsoleComponent, NewLinkEvent>(OnConsoleLink);
            SubscribeLocalEvent<GeneticsConsoleComponent, PortDisconnectedEvent>(OnConsolePortDisconnected);
            SubscribeLocalEvent<GeneticsConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleSelectBlockMessage>(OnSelectBlock);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleIrradiateBlockMessage>(OnIrradiateBlock);

            SubscribeLocalEvent<GeneticsComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<HulkGeneComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<HulkGeneComponent, PickupAttemptEvent>(OnHulkPickupAttempt);
            SubscribeLocalEvent<HulkGeneComponent, AccentGetEvent>(OnHulkAccent);
            SubscribeLocalEvent<HulkGeneComponent, IsEquippingAttemptEvent>(OnHulkEquipAttempt);
            SubscribeLocalEvent<HulkGeneComponent, IsEquippingTargetAttemptEvent>(OnHulkEquipTargetAttempt);
            SubscribeLocalEvent<HulkGeneComponent, IsUnequippingAttemptEvent>(OnHulkUnequipAttempt);
            SubscribeLocalEvent<HulkGeneComponent, IsUnequippingTargetAttemptEvent>(OnHulkUnequipTargetAttempt);
            SubscribeLocalEvent<HulkGeneComponent, DidEquipEvent>(OnHulkDidEquip);
            SubscribeLocalEvent<HulkGeneComponent, DidUnequipEvent>(OnHulkDidUnequip);
            SubscribeLocalEvent<HulkGeneComponent, StripAttemptEvent>(OnHulkStripAttempt);
            SubscribeLocalEvent<StrongGeneComponent, MeleeHitEvent>(OnStrongMeleeHit);
            SubscribeLocalEvent<ItemToggleComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);

            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleInjectMessage>(OnInjectMessage);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleEjectBeakerMessage>(OnEjectBeakerMessage);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleModifyGeneMessage>(OnModifyGeneMessage);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleStoreBufferMessage>(OnStoreBufferMessage);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleTransferBufferMessage>(OnTransferBufferMessage);
            SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleToggleScannerLockMessage>(OnToggleScannerLockMessage);
            SubscribeLocalEvent<GeneticsConsoleComponent, ComponentInit>(OnConsoleInit);
            SubscribeLocalEvent<GeneticsConsoleComponent, EntInsertedIntoContainerMessage>(OnBeakerInserted);
            SubscribeLocalEvent<GeneticsConsoleComponent, EntRemovedFromContainerMessage>(OnBeakerRemoved);
            SubscribeLocalEvent<MedicalScannerComponent, EntInsertedIntoContainerMessage>(OnScannerContainerChanged);
            SubscribeLocalEvent<MedicalScannerComponent, EntRemovedFromContainerMessage>(OnScannerContainerChanged);
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting); 
            SubscribeLocalEvent<GeneticSyringeComponent, AfterInjectEvent>(OnGeneticSyringeAfterInject);
            InitializeGenetics(); 
        }

        private void OnRoundStarting(RoundStartingEvent ev)
        {
             InitializeGenetics();
        }

        private void InitializeGenetics()
        {
            _geneMap.Clear();
            _geneTargets.Clear();
            _geneMinThresholds.Clear();
            _geneMinThresholdHex.Clear();

            var genes = _prototypeManager.EnumeratePrototypes<GenePrototype>().ToList();
            var availableBlocks = Enumerable.Range(0, SeHumanoidBlockIndex).ToList();

            foreach (var gene in genes)
            {
                if (availableBlocks.Count == 0) break;
                var blockIndex = _random.PickAndTake(availableBlocks);
                _geneMap[blockIndex] = gene.ID;

                if (TryParseHexBlock(gene.ActivationMin, out var minValue, out var minHex))
                {
                    _geneMinThresholds[gene.ID] = minValue;
                    _geneMinThresholdHex[gene.ID] = minHex;
                    _geneTargets[gene.ID] = minHex;
                }
                else
                {
                    _geneTargets[gene.ID] = GenerateRandomSingleHex();
                }
            }

            _geneMap[SeHumanoidBlockIndex] = "Humanoid";
            _geneTargets["Humanoid"] = "800";
        }

        private void OnMapInit(EntityUid uid, GeneticsComponent component, MapInitEvent args)
        {
            GenerateDNA(uid, component);
        }

        private void GenerateDNA(EntityUid uid, GeneticsComponent component)
        {
            component.UiDna.Clear();
            component.SeDna.Clear();

            HumanoidAppearanceComponent? humanoid = null;
            TryComp<HumanoidAppearanceComponent>(uid, out humanoid);

            for (int i = 0; i < UiBlockCount; i++)
            {
                string hexBlock;

                if (humanoid != null)
                {
                    hexBlock = GenerateGeneBlockFromAppearance(i, humanoid);
                }
                else
                {
                    hexBlock = GenerateRandomHexBlock();
                }

                component.UiDna[i] = hexBlock;
            }

            for (int i = 0; i < SeBlockCount; i++)
            {
                if (i == SeHumanoidBlockIndex)
                {
                    component.SeDna[i] = "7FF";
                }
                else
                {
                    
                    if (_geneMap.TryGetValue(i, out var geneId) &&
                        _geneMinThresholds.TryGetValue(geneId, out var min))
                    {
                        component.SeDna[i] = GenerateRandomHexBlockBelow(min);
                        continue;
                    }

                    var block = GenerateRandomHexBlock();
                    if (_geneMap.TryGetValue(i, out geneId) &&
                        _geneTargets.TryGetValue(geneId, out var target) &&
                        target.Length > 0)
                    {
                        
                        while (block.Length > 0 && block[0] == target[0])
                        {
                            block = GenerateRandomHexBlock();
                        }
                    }
                    component.SeDna[i] = block;
                }
            }
        }

        private string GenerateGeneBlockFromAppearance(int blockIndex, HumanoidAppearanceComponent humanoid)
        {
            if (blockIndex <= 2)
            {
                var hairColor = Color.Black;
                if (humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings))
                {
                    var firstMarking = hairMarkings.FirstOrDefault();
                    if (firstMarking != null && firstMarking.MarkingColors.Count > 0)
                        hairColor = firstMarking.MarkingColors[0];
                }

                return ColorToSingleHex(hairColor, blockIndex);
            }

            if (blockIndex <= 5)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 3);
            }

            if (blockIndex <= 8)
            {
                var facialHairColor = Color.Black;
                if (humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialMarkings))
                {
                    var firstMarking = facialMarkings.FirstOrDefault();
                    if (firstMarking != null && firstMarking.MarkingColors.Count > 0)
                        facialHairColor = firstMarking.MarkingColors[0];
                }

                return ColorToSingleHex(facialHairColor, blockIndex - 6);
            }

            if (blockIndex <= 11)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 9);
            }

            if (blockIndex == 12)
            {
                return SkinColorToHexBlock(humanoid.SkinColor);
            }

            if (blockIndex <= 15)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 13);
            }

            if (blockIndex <= 18)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 16);
            }

            if (blockIndex <= 21)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 19);
            }

            if (blockIndex <= 24)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 22);
            }

            if (blockIndex <= 27)
            {
                return ColorToSingleHex(Color.Black, blockIndex - 25);
            }

            if (blockIndex <= 30)
            {
                return ColorToSingleHex(humanoid.EyeColor, blockIndex - 28);
            }

            if (blockIndex == 31)
            {
                return humanoid.Gender switch
                {
                    Gender.Male => "23E",
                    Gender.Female => "23D",
                    _ => "801"
                };
            }

            if (blockIndex >= 32 && blockIndex <= 38)
            {
                return "000";
            }

            return GenerateRandomHexBlock();
        }

        private string ColorToHexBlock(Color color)
        {
            int r = color.RByte / 16;
            int g = color.GByte / 16;
            int b = color.BByte / 16;

            return r.ToString("X") + g.ToString("X") + b.ToString("X");
        }

        private string SkinColorToHexBlock(Color skinColor)
        {
            int r = skinColor.RByte;
            int g = skinColor.GByte;
            int b = skinColor.BByte;

            return (r / 16).ToString("X") + (g / 16).ToString("X") + (b / 16).ToString("X");
        }

        private string ColorToSingleHex(Color color, int channelIndex)
        {
            var value = channelIndex switch
            {
                0 => color.RByte,
                1 => color.GByte,
                _ => color.BByte
            };

            return (value / 16).ToString("X") + "00";
        }

        private bool IsGeneActive(int blockIndex, string hexValue)
        {
            if (_geneMap.TryGetValue(blockIndex, out var geneId))
            {
                if (_geneMinThresholds.TryGetValue(geneId, out var min))
                {
                    if (TryParseHexBlock(hexValue, out var val, out _))
                    {
                        var active = val >= min;
                        return active;
                    }
                    return false;
                }

                if (_geneTargets.TryGetValue(geneId, out var target))
                {
                    
                    var active = hexValue.Length > 0 && target.Length > 0 && hexValue[0] == target[0];
                    return active;
                }
            }
            return false;
        }

        private string GenerateRandomSingleHex()
        {
            return _random.Next(0, 16).ToString("X");
        }

        private string GenerateRandomHexBlock()
        {
            return GenerateRandomSingleHex() + GenerateRandomSingleHex() + GenerateRandomSingleHex();
        }

        private string GenerateRandomHexBlockBelow(int maxExclusive)
        {
            if (maxExclusive <= 0)
                return "000";

            var value = _random.Next(0, Math.Clamp(maxExclusive, 1, 0x1000));
            return value.ToString("X3");
        }

        private bool TryParseHexBlock(string? hex, out int value, out string normalized)
        {
            value = 0;
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            var cleaned = hex.Trim().ToUpperInvariant();
            if (cleaned.Length > 3)
                cleaned = cleaned.Substring(0, 3);
            cleaned = cleaned.PadLeft(3, '0');

            if (!int.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out value))
                return false;

            normalized = cleaned;
            return true;
        }

        private void OnSelectBlock(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleSelectBlockMessage args)
        {
            if (args.IsUiBlock)
                component.SelectedUiBlock = args.BlockIndex;
            else
                component.SelectedSeBlock = args.BlockIndex;

            UpdateUserInterface(uid, component);
        }

        private void OnIrradiateBlock(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleIrradiateBlockMessage args)
        {
            if (args.IsUiBlock)
                component.SelectedUiBlock = args.BlockIndex;
            else
                component.SelectedSeBlock = args.BlockIndex;

            if (component.GeneticScanner == null || !TryComp<MedicalScannerComponent>(component.GeneticScanner.Value, out var scanner))
                return;

            var body = scanner.BodyContainer.ContainedEntity;
            if (body == null || !TryComp<GeneticsComponent>(body, out var genetics))
                return;

            var dna = args.IsUiBlock ? genetics.UiDna : genetics.SeDna;

            if (!dna.ContainsKey(args.BlockIndex))
                return;

            dna[args.BlockIndex] = GenerateRandomHexBlock();
            Dirty(body.Value, genetics);

            if (args.IsUiBlock)
            {
                ApplyGeneticChanges(body.Value, genetics, args.BlockIndex);
            }
              else
              {
                  ApplyStructuralChanges(body.Value, genetics, args.BlockIndex, scanner);
                  UpdateInstability(body.Value, genetics);
                  ApplyGenePassiveEffectsNow(body.Value, genetics);
              }

            genetics.Instability += 5;

            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Radiation", 5); 
            _damageableSystem.TryChangeDamage(body.Value, damage, true);

            if (TryComp<DamageableComponent>(body.Value, out var damageable))
            {
                var radiationDamage = damageable.Damage.DamageDict.TryGetValue("Radiation", out var total) ? total.Float() : 0f;
                genetics.RadiationExposure = Math.Clamp((int) MathF.Round(radiationDamage), 0, 100);
            }

            UpdateUserInterface(uid, component);
        }

        private void OnConsoleMapInit(EntityUid uid, GeneticsConsoleComponent component, MapInitEvent args)
        {
            RecheckScannerLink(uid, component);
        }

        private void OnConsoleLink(EntityUid uid, GeneticsConsoleComponent component, NewLinkEvent args)
        {
            if (args.SourcePort != "MedicalScannerSender")
                return;

            if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner))
            {
                component.GeneticScanner = args.Sink;
                scanner.ConnectedConsole = uid;
            }

            UpdateUserInterface(uid, component);
        }

        private void OnConsolePortDisconnected(EntityUid uid, GeneticsConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port == "MedicalScannerSender")
                component.GeneticScanner = null;

            UpdateUserInterface(uid, component);
        }

        private void OnUiOpened(EntityUid uid, GeneticsConsoleComponent component, BoundUIOpenedEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void UpdateUserInterface(EntityUid uid, GeneticsConsoleComponent component)
        {
            RecheckScannerLink(uid, component);
            EnsureTransferBuffers(component);

            var syringe = _itemSlotsSystem.GetItemOrNull(uid, SharedGeneticsConsole.SyringeSlotId);
            var hasSyringe = syringe != null && TryComp<InjectorComponent>(syringe.Value, out _);

            MedicalScannerComponent? scanner = null;
            var hasScanner = component.GeneticScanner != null
                && TryComp<MedicalScannerComponent>(component.GeneticScanner.Value, out scanner);
            var scannerLocked = scanner != null && scanner.Locked;

            if (scanner != null)
            {
                var body = scanner.BodyContainer.ContainedEntity;
                if (body != null && TryComp<GeneticsComponent>(body, out var genetics))
                {
                    if (TryComp<DamageableComponent>(body, out var bodyDamageable))
                        UpdateRadiationExposure(genetics, bodyDamageable);

                    var name = Name(body.Value);
                    var isAlive = !_mobStateSystem.IsDead(body.Value);

                    float currentHealth = 0;
                    float maxHealth = 100;

                    if (TryComp<DamageableComponent>(body, out var healthDamageable) && TryComp<MobThresholdsComponent>(body, out var thresholds))
                    {
                         if (_mobThreshold.TryGetThresholdForState(body.Value, Content.Shared.Mobs.MobState.Dead, out var deadThreshold, thresholds))
                         {
                             maxHealth = (float)deadThreshold;
                             currentHealth = maxHealth - (float)healthDamageable.TotalDamage;
                         }
                    }

                    bool hasBeaker = false;
                    string beakerLabel = "";
                    int beakerCurrentVolume = 0;
                    int beakerMaxVolume = 0;

                    var beaker = _itemSlotsSystem.GetItemOrNull(uid, SharedGeneticsConsole.BeakerSlotId);
                    if (beaker != null)
                    {
                        hasBeaker = true;
                        if (TryComp<LabelComponent>(beaker.Value, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                            beakerLabel = label.CurrentLabel;
                        else
                              beakerLabel = Loc.GetString("genetics-console-no-label");

                        if (_solutionSystem.TryGetDrainableSolution(beaker.Value, out var soln, out var solution))
                        {
                            beakerCurrentVolume = (int)solution.Volume;
                            beakerMaxVolume = (int)solution.MaxVolume;
                        }
                    }

                    var state = new GeneticsConsoleBoundUserInterfaceState(name, hasScanner, scannerLocked, true, isAlive, currentHealth, maxHealth, genetics.UiDna, genetics.SeDna, genetics.Instability, genetics.RadiationExposure, component.SelectedUiBlock, component.SelectedSeBlock, hasBeaker, hasSyringe, beakerLabel, beakerCurrentVolume, beakerMaxVolume, BuildTransferBufferState(component));
                    _uiSystem.SetUiState(uid, GeneticsConsoleUiKey.Key, state);
                    return;
                }
            }

              _uiSystem.SetUiState(uid, GeneticsConsoleUiKey.Key, new GeneticsConsoleBoundUserInterfaceState(Loc.GetString("genetics-console-no-subject"), hasScanner, scannerLocked, false, false, 0, 100, new Dictionary<int, string>(), new Dictionary<int, string>(), 0, 0, -1, -1, hasSyringe: hasSyringe, transferBuffers: BuildTransferBufferState(component)));
        }

        private void EnsureTransferBuffers(GeneticsConsoleComponent component)
        {
            if (component.TransferBuffers.Count == GeneticsConsoleComponent.TransferBufferCount)
                return;

            component.TransferBuffers.Clear();
            for (var i = 0; i < GeneticsConsoleComponent.TransferBufferCount; i++)
            {
                component.TransferBuffers.Add(new GeneticsTransferBuffer());
            }
        }

        private List<GeneticsConsoleTransferBufferState> BuildTransferBufferState(GeneticsConsoleComponent component)
        {
            var states = new List<GeneticsConsoleTransferBufferState>(GeneticsConsoleComponent.TransferBufferCount);
            foreach (var buffer in component.TransferBuffers)
            {
                states.Add(new GeneticsConsoleTransferBufferState(buffer.SubjectName, buffer.HasUi, buffer.HasSe));
            }

            return states;
        }

        private void ApplyStructuralChanges(EntityUid uid, GeneticsComponent component, int changedBlock, MedicalScannerComponent scanner)
        {
            if (changedBlock != SeHumanoidBlockIndex)
                return;

            if (!component.SeDna.TryGetValue(changedBlock, out var hexBlock))
                return;

            if (!int.TryParse(hexBlock, System.Globalization.NumberStyles.HexNumber, null, out var value))
                return;

            var targetForm = "Human";
            if (value >= 0x800)
            {
                var isUnathi = false;
                if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
                    isUnathi = appearance.Species == "Reptilian";

                targetForm = isUnathi ? "Kobold" : "Monkey";
            }
            if (!_prototypeManager.TryIndex<PolymorphPrototype>(targetForm, out var prototype))
                return;

            var newEntity = _polymorphSystem.PolymorphEntity(uid, prototype.ID);
            if (newEntity != null)
            {
                _containerSystem.Insert(newEntity.Value, scanner.BodyContainer);

                if (TryComp<GeneticsComponent>(uid, out var sourceGenetics))
                {
                    var targetGenetics = EnsureComp<GeneticsComponent>(newEntity.Value);
                    targetGenetics.UiDna = new Dictionary<int, string>(sourceGenetics.UiDna);
                    targetGenetics.SeDna = new Dictionary<int, string>(sourceGenetics.SeDna);
                    targetGenetics.InnateGenes = new HashSet<string>(sourceGenetics.InnateGenes);
                    targetGenetics.Instability = sourceGenetics.Instability;
                    targetGenetics.RadiationExposure = sourceGenetics.RadiationExposure;
                }

                if (TryComp<DamageableComponent>(uid, out var sourceDamage) &&
                    TryComp<DamageableComponent>(newEntity.Value, out var targetDamage))
                {
                    var damageCopy = new DamageSpecifier(sourceDamage.Damage);
                    _damageableSystem.SetDamage((newEntity.Value, targetDamage), damageCopy);
                }
            }
        }

        private void ApplyGeneticChanges(EntityUid uid, GeneticsComponent component, int changedBlock)
        {
            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
                return;

            string hexBlock = component.UiDna[changedBlock];

            if (changedBlock <= 2)
            {
                ApplyHairColorFromGenes(uid, component, humanoid);
            }
            else if (changedBlock <= 5)
            {
                ApplySecondaryHairColorFromGenes(uid, component, humanoid);
            }
            else if (changedBlock <= 8)
            {
                ApplyBeardColorFromGenes(uid, component, humanoid);
            }
            else if (changedBlock <= 11)
            {
                ApplySecondaryBeardColorFromGenes(uid, component, humanoid);
            }
            else if (changedBlock == 12)
            {
                ApplySkinColorFromGeneBlock(uid, component, humanoid, hexBlock);
            }
            else if (changedBlock <= 15)
            {
                ApplyFurColorFromGenes(uid, component, humanoid);
            }
            else if (changedBlock <= 18)
            {
                ApplyMarkingCategoryColor(uid, humanoid, MarkingCategories.HeadTop, GetColorFromRgbBlocks(component, 16));
            }
            else if (changedBlock <= 21)
            {
                ApplyMarkingCategoryColor(uid, humanoid, MarkingCategories.Head, GetColorFromRgbBlocks(component, 19));
            }
            else if (changedBlock <= 24)
            {
                ApplyMarkingCategoryColor(uid, humanoid, MarkingCategories.Chest, GetColorFromRgbBlocks(component, 22));
            }
            else if (changedBlock <= 27)
            {
                ApplyMarkingCategoryColor(uid, humanoid, MarkingCategories.Tail, GetColorFromRgbBlocks(component, 25));
            }
            else if (changedBlock <= 30)
            {
                ApplyEyeColorFromGenes(uid, component, humanoid);
            }
            else if (changedBlock == 31)
            {
                ApplyGenderFromGeneBlock(uid, humanoid, hexBlock);
            }
            else if (changedBlock == 32)
            {
                ApplyFacialHairStyleFromGenes(uid, component, humanoid);
            }
            else if (changedBlock == 33)
            {
                ApplyHairStyleFromGenes(uid, component, humanoid);
            }
            else if (changedBlock == 34)
            {
                ApplyMarkingCategoryStyle(uid, humanoid, MarkingCategories.HeadTop, component);
            }
            else if (changedBlock == 35)
            {
                ApplyMarkingCategoryStyle(uid, humanoid, MarkingCategories.Head, component);
            }
            else if (changedBlock == 36)
            {
                ApplyMarkingCategoryStyle(uid, humanoid, MarkingCategories.Chest, component);
            }
            else if (changedBlock == 37)
            {
                ApplyMarkingCategoryStyle(uid, humanoid, MarkingCategories.Tail, component);
            }
            else if (changedBlock == 38)
            {
                ApplyRandomAdditionalMarkings(uid, humanoid);
            }

            UpdateInstability(uid, component);
        }

        private void ApplySecondaryHairColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            ApplyHairColorFromGenes(uid, component, humanoid, 3);
        }

        private void ApplySecondaryBeardColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            ApplyBeardColorFromGenes(uid, component, humanoid, 9);
        }

        private void ApplyFurColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            var color = GetColorFromRgbBlocks(component, 13);
            ApplyMarkingCategoryColor(uid, humanoid, MarkingCategories.Head, color);
        }

        private void ApplyHairStyleFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            var styleId = PickStyleFromGene(component, MarkingCategories.Hair, humanoid, fallbackId: "HairNormal");
            if (string.IsNullOrEmpty(styleId))
                return;

            EnsureMarkingSlot(uid, humanoid, MarkingCategories.Hair, styleId);
        }

        private void ApplyFacialHairStyleFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            var styleId = PickStyleFromGene(component, MarkingCategories.FacialHair, humanoid, fallbackId: "Shaved");
            if (string.IsNullOrEmpty(styleId))
                return;

            EnsureMarkingSlot(uid, humanoid, MarkingCategories.FacialHair, styleId);
        }

        private void ApplyMarkingCategoryStyle(EntityUid uid, HumanoidAppearanceComponent humanoid, MarkingCategories category, GeneticsComponent? component = null)
        {
            var styleId = PickStyleFromGene(component, category, humanoid, fallbackId: null);
            if (string.IsNullOrEmpty(styleId))
                return;

            EnsureMarkingSlot(uid, humanoid, category, styleId);
        }

        private void ApplyMarkingCategoryColor(EntityUid uid, HumanoidAppearanceComponent humanoid, MarkingCategories category, Color color)
        {
            if (!humanoid.MarkingSet.TryGetCategory(category, out var markings))
                return;

            var updated = new List<Marking>(markings.Count);
            foreach (var marking in markings)
            {
                var updatedMarking = new Marking(marking);
                for (int i = 0; i < updatedMarking.MarkingColors.Count; i++)
                {
                    updatedMarking.SetColor(i, color);
                }
                updated.Add(updatedMarking);
            }

            humanoid.MarkingSet.Markings[category] = updated;
            Dirty(uid, humanoid);
        }

        private string? PickStyleFromGene(GeneticsComponent? component, MarkingCategories category, HumanoidAppearanceComponent humanoid, string? fallbackId)
        {
            var styles = _markingManager.MarkingsByCategoryAndSpeciesAndSex(category, humanoid.Species, humanoid.Sex).Keys.ToList();
            if (styles.Count == 0)
                return fallbackId;

            if (component == null)
                return _random.Pick(styles);

            var blockIndex = category switch
            {
                MarkingCategories.FacialHair => 32,
                MarkingCategories.Hair => 33,
                MarkingCategories.HeadTop => 34,
                MarkingCategories.Head => 35,
                MarkingCategories.Chest => 36,
                MarkingCategories.Tail => 37,
                _ => -1
            };

            if (blockIndex == -1 || !component.UiDna.TryGetValue(blockIndex, out var hexBlock))
                return _random.Pick(styles);

            var value = int.Parse(hexBlock, System.Globalization.NumberStyles.HexNumber);
            var index = Math.Abs(value) % styles.Count;
            return styles[index];
        }

        private void EnsureMarkingSlot(EntityUid uid, HumanoidAppearanceComponent humanoid, MarkingCategories category, string markingId)
        {
            if (!_markingManager.MarkingsByCategory(category).TryGetValue(markingId, out var markingPrototype))
                return;

            if (!humanoid.MarkingSet.TryGetCategory(category, out var markings) || markings.Count == 0)
            {
                var marking = markingPrototype.AsMarking();
                var color = GetCategoryColor(humanoid, category);
                for (var i = 0; i < marking.MarkingColors.Count; i++)
                {
                    marking.SetColor(i, color);
                }

                humanoid.MarkingSet.AddBack(category, marking);
                Dirty(uid, humanoid);
                return;
            }

            _serverHumanoidAppearance.SetMarkingId(uid, category, 0, markingId, humanoid);
        }

        private Color GetCategoryColor(HumanoidAppearanceComponent humanoid, MarkingCategories category)
        {
            if (humanoid.MarkingSet.TryGetCategory(category, out var markings))
            {
                var first = markings.FirstOrDefault();
                if (first != null && first.MarkingColors.Count > 0)
                    return first.MarkingColors[0];
            }

            return new Color(_random.Next(0, 16) * 16, _random.Next(0, 16) * 16, _random.Next(0, 16) * 16);
        }

        private void ApplyRandomAdditionalMarkings(EntityUid uid, HumanoidAppearanceComponent humanoid)
        {
            var categories = new[]
            {
                MarkingCategories.HeadTop,
                MarkingCategories.HeadSide,
                MarkingCategories.Head,
                MarkingCategories.Chest,
                MarkingCategories.Arms,
                MarkingCategories.Legs,
                MarkingCategories.Tail,
                MarkingCategories.Overlay
            };

            foreach (var category in categories)
            {
                var styleId = PickStyleFromGene(null, category, humanoid, fallbackId: null);
                if (string.IsNullOrEmpty(styleId))
                    continue;

                EnsureMarkingSlot(uid, humanoid, category, styleId);
            }
        }

        private void ApplyHairColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            ApplyHairColorFromGenes(uid, component, humanoid, 0);
        }

        private void ApplyHairColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid, int blockStart)
        {
            var color = GetColorFromRgbBlocks(component, blockStart);

            if (humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings))
            {
                var updated = new List<Marking>(hairMarkings.Count);
                foreach (var marking in hairMarkings)
                {
                    var updatedMarking = new Marking(marking);
                    for (int i = 0; i < updatedMarking.MarkingColors.Count; i++)
                    {
                        updatedMarking.SetColor(i, color);
                    }
                    updated.Add(updatedMarking);
                }

                humanoid.MarkingSet.Markings[MarkingCategories.Hair] = updated;
            }

            Dirty(uid, humanoid);
        }

        private void ApplyBeardColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            ApplyBeardColorFromGenes(uid, component, humanoid, 6);
        }

        private void ApplyBeardColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid, int blockStart)
        {
            var color = GetColorFromRgbBlocks(component, blockStart);

            if (humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialMarkings))
            {
                var updated = new List<Marking>(facialMarkings.Count);
                foreach (var marking in facialMarkings)
                {
                    var updatedMarking = new Marking(marking);
                    for (int i = 0; i < updatedMarking.MarkingColors.Count; i++)
                    {
                        updatedMarking.SetColor(i, color);
                    }
                    updated.Add(updatedMarking);
                }

                humanoid.MarkingSet.Markings[MarkingCategories.FacialHair] = updated;
            }

            Dirty(uid, humanoid);
        }

        private void ApplyEyeColorFromGenes(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid)
        {
            var color = GetColorFromRgbBlocks(component, 28);
            humanoid.EyeColor = color;
            Dirty(uid, humanoid);
        }

        private void ApplySkinColorFromGeneBlock(EntityUid uid, GeneticsComponent component, HumanoidAppearanceComponent humanoid, string hexBlock)
        {
            var skinColor = HexBlockToColor(hexBlock);
            _humanoidSystem.SetSkinColor(uid, skinColor, sync: true, humanoid: humanoid);
        }

        private void ApplyGenderFromGeneBlock(EntityUid uid, HumanoidAppearanceComponent humanoid, string hexBlock)
        {
            if (int.TryParse(hexBlock, System.Globalization.NumberStyles.HexNumber, null, out var val))
            {
                Gender newGender;
                if (val >= 0x801)
                    newGender = Gender.Neuter;
                else if (val >= 0x23E)
                    newGender = Gender.Male;
                else
                    newGender = Gender.Female;

                if (humanoid.Gender != newGender)
                {
                    humanoid.Gender = newGender;
                    Dirty(uid, humanoid);
                }
            }
        }

        private Color GetColorFromGeneBlock(GeneticsComponent component, int blockIndex)
        {
            if (!component.UiDna.TryGetValue(blockIndex, out var hexBlock))
                return Color.White;

            return HexBlockToColor(hexBlock);
        }

        private Color GetColorFromRgbBlocks(GeneticsComponent component, int blockStart)
        {
            if (!component.UiDna.TryGetValue(blockStart, out var redBlock) ||
                !component.UiDna.TryGetValue(blockStart + 1, out var greenBlock) ||
                !component.UiDna.TryGetValue(blockStart + 2, out var blueBlock))
            {
                return Color.White;
            }

            var r = HexCharToColorValue(redBlock[0]);
            var g = HexCharToColorValue(greenBlock[0]);
            var b = HexCharToColorValue(blueBlock[0]);

            return new Color(r, g, b);
        }

        private int HexCharToColorValue(char hexChar)
        {
            var value = int.Parse(hexChar.ToString(), System.Globalization.NumberStyles.HexNumber);
            return value * 16;
        }

        private Color HexBlockToColor(string hexBlock)
        {
            if (hexBlock.Length < 3)
                return Color.White;

            int r = int.Parse(hexBlock[0].ToString(), System.Globalization.NumberStyles.HexNumber) * 16;
            int g = int.Parse(hexBlock[1].ToString(), System.Globalization.NumberStyles.HexNumber) * 16;
            int b = int.Parse(hexBlock[2].ToString(), System.Globalization.NumberStyles.HexNumber) * 16;

            return new Color(r, g, b);
        }

        public override void Update(float frameTime)
        {
             base.Update(frameTime);
            if (_timing.CurTime < _nextTick) return;
            _nextTick = _timing.CurTime + TimeSpan.FromSeconds(5);

            var enumerator = EntityQueryEnumerator<GeneticsComponent, DamageableComponent>();
            while (enumerator.MoveNext(out var uid, out var genetics, out var damageable))
            {
                if (_mobStateSystem.IsDead(uid)) continue;
                UpdateRadiationExposure(genetics, damageable);
                UpdateInstability(uid, genetics);
                ApplyInstabilityEffects(uid, genetics, damageable);
                ApplyGenePassiveEffects(uid, genetics, damageable);
            }
        }

        private TimeSpan _nextTick;

        private void UpdateInstability(EntityUid uid, GeneticsComponent component)
        {

            int stability = 0;
            foreach (var (blockIndex, geneId) in _geneMap)
            {
                if (!component.SeDna.TryGetValue(blockIndex, out var hexVal))
                    continue;
                if (geneId == "Humanoid")
                    continue;
                if (!IsGeneActive(blockIndex, hexVal))
                    continue;
                if (_prototypeManager.TryIndex<GenePrototype>(geneId, out var gene))
                    stability += gene.Instability;
            }

            foreach (var geneId in component.InnateGenes)
            {
                if (_prototypeManager.TryIndex<GenePrototype>(geneId, out var gene))
                    stability += gene.Instability;
            }

            component.Instability = Math.Max(0, stability);
        }

        private void UpdateRadiationExposure(GeneticsComponent component, DamageableComponent damageable)
        {
            var radiationDamage = damageable.Damage.DamageDict.TryGetValue("Radiation", out var total) ? total.Float() : 0f;
            component.RadiationExposure = Math.Clamp((int) MathF.Round(radiationDamage), 0, 100);
        }

        private void ApplyInstabilityEffects(EntityUid uid, GeneticsComponent component, DamageableComponent damageable)
        {
            int instability = component.Instability;
            
            if (instability < 25)
                return;

            
            if (instability < 40)
            {
                if (_random.Prob(0.25f))
                {
                    var damage = new DamageSpecifier();
                    damage.DamageDict.Add("Heat", _random.Next(1, 3));
                    _damageableSystem.TryChangeDamage(uid, damage, true);
                }
                return;
            }

            
            if (instability < 70)
            {
                var damage = new DamageSpecifier();
                damage.DamageDict.Add("Cellular", _random.Next(1, 4));
                _damageableSystem.TryChangeDamage(uid, damage, true);

                if (_random.Prob(0.4f))
                {
                    _stunSystem.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(_random.Next(2, 5)));
                }
                return;
            }

            
            {
                var damage = new DamageSpecifier();
                damage.DamageDict.Add("Cellular", _random.Next(8, 16));
                _damageableSystem.TryChangeDamage(uid, damage, true);

                if (_random.Prob(0.7f))
                {
                    _stunSystem.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(_random.Next(4, 8)));
                }
            }
        }

        private void ApplyGeneEffect(EntityUid uid, string geneId, bool isActive, GeneticsComponent component)
        {
            switch (geneId)
            {
                case "Blindness":
                    if (isActive)
                    {
                        var blindness = EnsureComp<PermanentBlindnessComponent>(uid);
                        blindness.Blindness = 0;
                    }
                    else
                    {
                        RemCompDeferred<PermanentBlindnessComponent>(uid);
                    }
                    break;
                case "Telekinesis":
                    if (isActive)
                    {
                        EnsureComp<Content.Shared._OpenSpace.Genetics.Telekinesis.TelekinesisVisualComponent>(uid);
                        if (!HasComp<Content.Shared._Starlight.Computers.RemoteEye.RemoteEyeActorComponent>(uid))
                            _tagSystem.AddTag(uid, "TelekinesisInteractionRange");
                    }
                    else
                    {
                        _tagSystem.RemoveTag(uid, "TelekinesisInteractionRange");
                        RemCompDeferred<Content.Shared._OpenSpace.Genetics.Telekinesis.TelekinesisVisualComponent>(uid);
                    }
                    break;
                case "Humanoid":
                    
                    break;
            }
        }

        private void ApplyGenePassiveEffectsNow(EntityUid uid, GeneticsComponent component)
        {
            if (TryComp<DamageableComponent>(uid, out var damageable))
                ApplyGenePassiveEffects(uid, component, damageable);
        }

        private void ApplyGenePassiveEffects(EntityUid uid, GeneticsComponent component, DamageableComponent damageable)
        {
            
            if (IsGeneActive("Midget", component))
            {
                if (!TryComp<MidgetGeneComponent>(uid, out var midget))
                {
                    midget = EnsureComp<MidgetGeneComponent>(uid);
                    ApplyMidget(uid, midget);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-midget-activate"), uid, uid, PopupType.Medium);
                }
            }
            else if (TryComp<MidgetGeneComponent>(uid, out var midgetComp))
            {
                RevertMidget(uid, midgetComp);
                RemCompDeferred<MidgetGeneComponent>(uid);
            }

            
            if (IsGeneActive("NoBreathing", component))
            {
                if (!TryComp<NoBreathingGeneComponent>(uid, out var noBreath))
                {
                    noBreath = EnsureComp<NoBreathingGeneComponent>(uid);
                    noBreath.HadRespirator = HasComp<RespiratorComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-nobreathing-activate"), uid, uid, PopupType.Medium);
                }

                if (noBreath.HadRespirator && HasComp<RespiratorComponent>(uid))
                    RemCompDeferred<RespiratorComponent>(uid);
            }
            else if (TryComp<NoBreathingGeneComponent>(uid, out var noBreathComp))
            {
                if (noBreathComp.HadRespirator)
                    EnsureComp<RespiratorComponent>(uid);
                RemCompDeferred<NoBreathingGeneComponent>(uid);
            }

            
            if (IsGeneActive("NoPrints", component))
            {
                if (!TryComp<NoPrintsGeneComponent>(uid, out _))
                {
                    EnsureComp<NoPrintsGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-noprints-activate"), uid, uid, PopupType.Medium);
                }
                EnsureComp<FingerprintMaskComponent>(uid);
            }
            else
            {
                if (HasComp<NoPrintsGeneComponent>(uid))
                {
                    RemCompDeferred<NoPrintsGeneComponent>(uid);
                    RemCompDeferred<FingerprintMaskComponent>(uid);
                }
            }

            
            if (IsGeneActive("PsyResist", component))
            {
                if (!TryComp<PsyResistComponent>(uid, out _))
                {
                    EnsureComp<PsyResistComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-psyresist-activate"), uid, uid, PopupType.Medium);
                }
            }
            else if (TryComp<PsyResistComponent>(uid, out _))
            {
                RemCompDeferred<PsyResistComponent>(uid);
            }

            
            if (IsGeneActive("Cryokinesis", component))
            {
                if (!TryComp<CryokinesisComponent>(uid, out _))
                {
                    EnsureComp<CryokinesisComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-cryokinesis-activate"), uid, uid, PopupType.Medium);
                }
            }
            else
            {
                RemCompDeferred<CryokinesisComponent>(uid);
            }

            
            if (IsGeneActive("MatterEater", component))
            {
                if (!TryComp<MatterEaterComponent>(uid, out _))
                {
                    EnsureComp<MatterEaterComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-matter-eater-activate"), uid, uid, PopupType.Medium);
                }
            }
            else
            {
                RemCompDeferred<MatterEaterComponent>(uid);
            }

            
            if (IsGeneActive("Jumpy", component))
            {
                if (!TryComp<JumpyComponent>(uid, out _))
                {
                    EnsureComp<JumpyComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-jumpy-activate"), uid, uid, PopupType.Medium);
                }
            }
            else
            {
                RemCompDeferred<JumpyComponent>(uid);
            }

            
            if (IsGeneActive("EmpathicThought", component))
            {
                if (!TryComp<EmpathicThoughtComponent>(uid, out _))
                {
                    EnsureComp<EmpathicThoughtComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-empathic-activate"), uid, uid, PopupType.Medium);
                }
            }
            else
            {
                RemCompDeferred<EmpathicThoughtComponent>(uid);
            }

            
            if (IsGeneActive("FarVision", component))
            {
                if (!TryComp<FarVisionComponent>(uid, out _))
                {
                    EnsureComp<FarVisionComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-farvision-activate"), uid, uid, PopupType.Medium);
                }
            }
            else
            {
                RemCompDeferred<FarVisionComponent>(uid);
            }

            
            if (IsGeneActive("HeatResistance", component))
            {
                if (!TryComp<HeatResistanceGeneComponent>(uid, out var heatResist))
                {
                    heatResist = EnsureComp<HeatResistanceGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-heatresist-activate"), uid, uid, PopupType.Medium);
                }

                if (!TryComp<TemperatureProtectionComponent>(uid, out var tempProt))
                {
                    tempProt = EnsureComp<TemperatureProtectionComponent>(uid);
                    heatResist.AddedTempProtection = true;
                    heatResist.OriginalHeating = tempProt.HeatingCoefficient;
                    heatResist.OriginalCooling = tempProt.CoolingCoefficient;
                }
                else if (!heatResist.AddedTempProtection)
                {
                    heatResist.OriginalHeating = tempProt.HeatingCoefficient;
                    heatResist.OriginalCooling = tempProt.CoolingCoefficient;
                }

                _temperatureSystem.SetHeatProtection(tempProt, 0f);

                if (!HasComp<PressureImmunityComponent>(uid))
                {
                    EnsureComp<PressureImmunityComponent>(uid);
                    heatResist.AddedPressureImmunity = true;
                }
            }
            else if (TryComp<HeatResistanceGeneComponent>(uid, out var heatResistComp))
            {
                if (TryComp<TemperatureProtectionComponent>(uid, out var tempProt))
                {
                    _temperatureSystem.SetHeatProtection(tempProt, heatResistComp.OriginalHeating);
                    _temperatureSystem.SetColdProtection(tempProt, heatResistComp.OriginalCooling);

                    if (heatResistComp.AddedTempProtection)
                        RemCompDeferred<TemperatureProtectionComponent>(uid);
                }

                if (heatResistComp.AddedPressureImmunity)
                    RemCompDeferred<PressureImmunityComponent>(uid);

                RemCompDeferred<HeatResistanceGeneComponent>(uid);
            }

            
            if (IsGeneActive("ShockImmunity", component))
            {
                if (!TryComp<ShockImmunityGeneComponent>(uid, out var shock))
                {
                    shock = EnsureComp<ShockImmunityGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-shockimmunity-activate"), uid, uid, PopupType.Medium);
                }

                if (!TryComp<InsulatedComponent>(uid, out var insulated))
                {
                    insulated = EnsureComp<InsulatedComponent>(uid);
                    shock.AddedInsulation = true;
                    shock.OriginalCoefficient = insulated.Coefficient;
                }
                else if (!shock.AddedInsulation)
                {
                    shock.OriginalCoefficient = insulated.Coefficient;
                }

                _electrocutionSystem.SetInsulatedSiemensCoefficient(uid, 0f, insulated);
            }
            else if (TryComp<ShockImmunityGeneComponent>(uid, out var shockComp))
            {
                if (TryComp<InsulatedComponent>(uid, out var insulated))
                {
                    _electrocutionSystem.SetInsulatedSiemensCoefficient(uid, shockComp.OriginalCoefficient, insulated);
                    if (shockComp.AddedInsulation)
                        RemCompDeferred<InsulatedComponent>(uid);
                }

                RemCompDeferred<ShockImmunityGeneComponent>(uid);
            }

            
            if (IsGeneActive("Sober", component))
            {
                if (!TryComp<SoberGeneComponent>(uid, out _))
                {
                    EnsureComp<SoberGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-sober-activate"), uid, uid, PopupType.Medium);

                    if (_statusEffects.TryGetTime(uid, "Drunk", out var drunkTime))
                    {
                        var remaining = drunkTime.Value.Item2 - _timing.CurTime;
                        if (remaining > TimeSpan.Zero)
                            _statusEffects.TrySetTime(uid, "Drunk", remaining / 2);
                    }
                }
            }
            else if (HasComp<SoberGeneComponent>(uid))
            {
                RemCompDeferred<SoberGeneComponent>(uid);
            }

            
            if (IsGeneActive("Strong", component))
            {
                if (!TryComp<StrongGeneComponent>(uid, out _))
                {
                    EnsureComp<StrongGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-strong-activate"), uid, uid, PopupType.Medium);
                }
            }
            else if (HasComp<StrongGeneComponent>(uid))
            {
                RemCompDeferred<StrongGeneComponent>(uid);
            }

            
            
            if (IsGeneActive("Regeneration", component))
            {
                if (!TryComp<RegenerationGeneComponent>(uid, out _))
                {
                    EnsureComp<RegenerationGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-regeneration-activate"), uid, uid, PopupType.Medium);
                }

                var regen = new DamageSpecifier();
                regen.DamageDict.Add("Blunt", -1);
                regen.DamageDict.Add("Slash", -1);
                regen.DamageDict.Add("Piercing", -1);
                regen.DamageDict.Add("Heat", -1);
                regen.DamageDict.Add("Cold", -1);
                _damageableSystem.TryChangeDamage(uid, regen, true, false, uid);
            }
            else if (HasComp<RegenerationGeneComponent>(uid))
            {
                RemCompDeferred<RegenerationGeneComponent>(uid);
            }

            
            
            if (IsGeneActive("Epilepsy", component))
            {
                if (!TryComp<EpilepsyGeneComponent>(uid, out _))
                {
                    EnsureComp<EpilepsyGeneComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-epilepsy-activate"), uid, uid, PopupType.Medium);
                }

                if (_random.Prob(0.25f))
                {
                    _statusEffects.TryAddStatusEffect<TemporaryBlindnessComponent>(uid, "TemporaryBlindness", TimeSpan.FromSeconds(4), true);
                    _jitterSystem.DoJitter(uid, TimeSpan.FromSeconds(6), true, amplitude: 5f, frequency: 10f);
                    _stunSystem.TryKnockdown(uid, TimeSpan.FromSeconds(3), true);
                    _stunSystem.TryAddStunDuration(uid, TimeSpan.FromSeconds(2));

                    
                    var seizureDmg = new DamageSpecifier();
                    seizureDmg.DamageDict.Add("Blunt", _random.Next(1, 5));
                    _damageableSystem.TryChangeDamage(uid, seizureDmg, true);

                    _popupSystem.PopupEntity("You collapse as a violent seizure takes hold!", uid, uid, PopupType.LargeCaution);
                    _popupSystem.PopupEntity(Name(uid) + " collapses, convulsing violently!", uid, PopupType.MediumCaution);
                }
                else
                {
                    
                    _jitterSystem.DoJitter(uid, TimeSpan.FromSeconds(1), true, amplitude: 1f, frequency: 2f);
                }
            }
            else if (HasComp<EpilepsyGeneComponent>(uid))
            {
                RemCompDeferred<EpilepsyGeneComponent>(uid);
            }

            
            
            
            if (IsGeneActive("Hulk", component))
            {
                var hulk = EnsureComp<HulkGeneComponent>(uid);
                if (hulk.LifeStage == ComponentLifeStage.Added)
                      _popupSystem.PopupEntity(Loc.GetString("genetics-hulk-activate"), uid, uid, PopupType.Medium);

                
                if (!hulk.IsTransformed)
                {
                    
                    var isUnathi = false;
                    if (TryComp<HumanoidAppearanceComponent>(uid, out var hulkAppearance))
                        isUnathi = hulkAppearance.Species == "Reptilian";

                    
                    var isClown = IsGeneActive("Epilepsy", component);

                    if (isUnathi)
                        hulk.Variant = "Godzilla";
                    else if (isClown)
                        hulk.Variant = "HonkChampion";
                    else
                        hulk.Variant = "Hulk";

                    
                    if (_mobThreshold.TryGetThresholdForState(uid, Content.Shared.Mobs.MobState.Dead, out var deadTh))
                        hulk.OriginalDeadThreshold = (float)deadTh;
                    else
                        hulk.OriginalDeadThreshold = 200f;

                    if (_mobThreshold.TryGetThresholdForState(uid, Content.Shared.Mobs.MobState.Critical, out var critTh))
                        hulk.OriginalCritThreshold = (float)critTh;
                    else
                        hulk.OriginalCritThreshold = 100f;

                    if (TryComp<MovementSpeedModifierComponent>(uid, out var moveMod))
                    {
                        hulk.OriginalSprintSpeed = moveMod.BaseSprintSpeed;
                        hulk.OriginalWalkSpeed = moveMod.BaseWalkSpeed;
                        hulk.OriginalAcceleration = moveMod.BaseAcceleration;
                    }
                    else
                    {
                        hulk.OriginalSprintSpeed = 4.5f;
                        hulk.OriginalWalkSpeed = 2.5f;
                        hulk.OriginalAcceleration = 20f;
                    }

                    if (TryComp<HumanoidAppearanceComponent>(uid, out var skinComp))
                        hulk.OriginalSkinColor = skinComp.SkinColor;

                    hulk.OriginalScale = _scaleVisuals.GetSpriteScale(uid);

                    
                    float newDeadThreshold;
                    float newCritThreshold;
                    float newSprintSpeed;
                    float newWalkSpeed;
                    Color skinColor;

                    switch (hulk.Variant)
                    {
                        case "Godzilla":
                            newDeadThreshold = 315f;
                            newCritThreshold = 315f; 
                            newSprintSpeed = 1.5f * 1.8f; 
                            newWalkSpeed = 1.5f;
                            skinColor = new Color(0.1f, 0.6f, 0.1f);
                            break;
                        case "HonkChampion":
                            newDeadThreshold = 175f;
                            newCritThreshold = 175f; 
                            newSprintSpeed = 4.5f; 
                            newWalkSpeed = 2.5f;
                            skinColor = new Color(1f, 0.8f, 0.2f); 
                            break;
                        default: 
                            newDeadThreshold = 280f;
                            newCritThreshold = 280f; 
                            newSprintSpeed = 1.25f * 1.8f; 
                            newWalkSpeed = 1.25f;
                            skinColor = new Color(0.2f, 0.8f, 0.2f); 
                            break;
                    }

                    
                    _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(newDeadThreshold), Content.Shared.Mobs.MobState.Dead);
                    _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(newCritThreshold), Content.Shared.Mobs.MobState.Critical);

                    
                    if (TryComp<MovementSpeedModifierComponent>(uid, out var speedComp))
                    {
                        _movementSpeedModifier.ChangeBaseSpeed(uid, newWalkSpeed, newSprintSpeed, 20f, speedComp);
                    }

                    
                    if (TryComp<HumanoidAppearanceComponent>(uid, out var skinHumanoid))
                    {
                        _humanoidSystem.SetSkinColor(uid, skinColor, sync: true, verify: false, humanoid: skinHumanoid);
                        Dirty(uid, skinHumanoid);
                    }

                    
                    var newScale = new Vector2(1.5f, 1.5f);
                    _scaleVisuals.SetSpriteScale(uid, newScale);

                    var scaleFactor = 1f;
                    if (Math.Abs(hulk.OriginalScale.X) > float.Epsilon)
                        scaleFactor = (newScale.X / hulk.OriginalScale.X) * 0.9f;

                    hulk.FixtureScaleFactor = scaleFactor;
                    if (Math.Abs(scaleFactor - 1f) > float.Epsilon)
                        _physics.ScaleFixtures(uid, scaleFactor);

                    
                    if (TryComp<HandsComponent>(uid, out var hands))
                    {
                        foreach (var hand in hands.Hands.Keys)
                        {
                            _handsSystem.TryDrop((uid, hands), hand, checkActionBlocker: false);
                        }
                    }

                     RemoveProvidedItemActions(uid);
                     SuppressNonHulkActions(uid, hulk);

                     hulk.IsTransformed = true;
                    Dirty(uid, hulk);
                    _popupSystem.PopupEntity(Name(uid) + " transforms into " + hulk.Variant + "!", uid, PopupType.LargeCaution);
                }

                
                float regenAmount;
                switch (hulk.Variant)
                {
                    case "HonkChampion":
                        regenAmount = 24f;
                        break;
                    default: 
                        regenAmount = 6f;
                        break;
                }

                
                var hulkRegen = new DamageSpecifier();
                hulkRegen.DamageDict.Add("Blunt", (int)-regenAmount / 3);
                hulkRegen.DamageDict.Add("Slash", (int)-regenAmount / 3);
                hulkRegen.DamageDict.Add("Heat", (int)-regenAmount / 3);
                _damageableSystem.TryChangeDamage(uid, hulkRegen, true, false, uid);

                
                if (_mobStateSystem.IsDead(uid) || _mobStateSystem.IsCritical(uid))
                {
                    RevertHulkTransformation(uid, hulk);
                    RemoveGene(uid, component, "Hulk");
                }
            }
            else
            {
                
                if (TryComp<HulkGeneComponent>(uid, out var hulkComp) && hulkComp.IsTransformed)
                {
                    RevertHulkTransformation(uid, hulkComp);
                }
            }

            
            
            if (IsGeneActive("Blindness", component))
            {
                if (!HasComp<PermanentBlindnessComponent>(uid))
                {
                    EnsureComp<PermanentBlindnessComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-blindness-activate"), uid, uid, PopupType.LargeCaution);
                }
            }
            else
            {
                if (HasComp<PermanentBlindnessComponent>(uid))
                {
                    RemCompDeferred<PermanentBlindnessComponent>(uid);
                    _popupSystem.PopupEntity(Loc.GetString("genetics-blindness-deactivate"), uid, uid, PopupType.Medium);
                }
            }

            
            
            
            if (IsGeneActive("Telekinesis", component))
            {
                if (!HasComp<Content.Shared._OpenSpace.Genetics.Telekinesis.TelekinesisVisualComponent>(uid))
                {
                    EnsureComp<Content.Shared._OpenSpace.Genetics.Telekinesis.TelekinesisVisualComponent>(uid);
                      _popupSystem.PopupEntity(Loc.GetString("genetics-telekinesis-activate"), uid, uid, PopupType.Medium);
                }
                if (HasComp<Content.Shared._Starlight.Computers.RemoteEye.RemoteEyeActorComponent>(uid))
                {
                    _tagSystem.RemoveTag(uid, "TelekinesisInteractionRange");
                }
                else
                {
                    _tagSystem.AddTag(uid, "TelekinesisInteractionRange");
                }
            }
            else
            {
                _tagSystem.RemoveTag(uid, "TelekinesisInteractionRange");
                RemCompDeferred<Content.Shared._OpenSpace.Genetics.Telekinesis.TelekinesisVisualComponent>(uid);
            }
        }

        private void RevertHulkTransformation(EntityUid uid, HulkGeneComponent hulk)
        {
            if (!hulk.IsTransformed)
                return;

            
            _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(hulk.OriginalDeadThreshold), Content.Shared.Mobs.MobState.Dead);
            _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(hulk.OriginalCritThreshold), Content.Shared.Mobs.MobState.Critical);

            
            if (TryComp<MovementSpeedModifierComponent>(uid, out var speedComp))
            {
                _movementSpeedModifier.ChangeBaseSpeed(uid, hulk.OriginalWalkSpeed, hulk.OriginalSprintSpeed, hulk.OriginalAcceleration, speedComp);
            }

            
            if (TryComp<HumanoidAppearanceComponent>(uid, out var skinHumanoid))
            {
                _humanoidSystem.SetSkinColor(uid, hulk.OriginalSkinColor, sync: true, verify: false, humanoid: skinHumanoid);
                Dirty(uid, skinHumanoid);
            }

            
            _scaleVisuals.SetSpriteScale(uid, hulk.OriginalScale);
            if (Math.Abs(hulk.FixtureScaleFactor - 1f) > float.Epsilon &&
                Math.Abs(hulk.FixtureScaleFactor) > float.Epsilon)
            {
                _physics.ScaleFixtures(uid, 1f / hulk.FixtureScaleFactor);
            }
            hulk.FixtureScaleFactor = 1f;

            hulk.IsTransformed = false;
            Dirty(uid, hulk);
            _popupSystem.PopupEntity(Name(uid) + " returns to their human form.", uid, PopupType.Medium);

              RestoreSuppressedActions(uid, hulk);
              RestoreProvidedItemActions(uid);
          }

        private void OnHulkPickupAttempt(EntityUid uid, HulkGeneComponent component, ref PickupAttemptEvent args)
        {
            if (component.IsTransformed)
                args.Cancel();
        }

        private void OnHulkAccent(EntityUid uid, HulkGeneComponent component, ref AccentGetEvent args)
        {

            var message = args.Message;
            if (string.IsNullOrEmpty(message))
                return;

            args.Message = message.ToUpperInvariant() + "!!!";
        }

        private void OnHulkEquipAttempt(EntityUid uid, HulkGeneComponent component, ref IsEquippingAttemptEvent args)
        {
            if (component.IsTransformed)
                args.Cancel();
        }

        private void OnHulkEquipTargetAttempt(EntityUid uid, HulkGeneComponent component, ref IsEquippingTargetAttemptEvent args)
        {
            if (component.IsTransformed)
                args.Cancel();
        }

        private void OnHulkUnequipAttempt(EntityUid uid, HulkGeneComponent component, ref IsUnequippingAttemptEvent args)
        {
            if (component.IsTransformed)
                args.Cancel();
        }

        private void OnHulkUnequipTargetAttempt(EntityUid uid, HulkGeneComponent component, ref IsUnequippingTargetAttemptEvent args)
        {
            if (component.IsTransformed)
                args.Cancel();
        }

        private void OnHulkStripAttempt(EntityUid uid, HulkGeneComponent component, ref StripAttemptEvent args)
        {
            if (component.IsTransformed)
                args.Cancel();
        }

        private void OnHulkDidEquip(EntityUid uid, HulkGeneComponent component, ref DidEquipEvent args)
        {
            if (!component.IsTransformed)
                return;

            EnsureHulkScale(uid, component);
            RemoveProvidedItemActions(uid);
            SuppressNonHulkActions(uid, component);
        }

        private void OnHulkDidUnequip(EntityUid uid, HulkGeneComponent component, ref DidUnequipEvent args)
        {
            if (!component.IsTransformed)
                return;

            EnsureHulkScale(uid, component);
            RemoveProvidedItemActions(uid);
            SuppressNonHulkActions(uid, component);
        }

        private void SuppressNonHulkActions(EntityUid uid, HulkGeneComponent hulk)
        {
            if (!TryComp<ActionsComponent>(uid, out var actions))
                return;

            hulk.SuppressedActions.Clear();
            foreach (var actionId in actions.Actions.ToArray())
            {
                if (!TryComp<ActionComponent>(actionId, out var action))
                    continue;

                var protoId = MetaData(actionId).EntityPrototype?.ID;
                if (protoId != null && HulkAllowedActionPrototypes.Contains(protoId))
                    continue;

                _actionsSystem.RemoveAction((uid, actions), (actionId, action));
                hulk.SuppressedActions.Add(actionId);
            }
        }

        private void RestoreSuppressedActions(EntityUid uid, HulkGeneComponent hulk)
        {
            if (hulk.SuppressedActions.Count == 0)
                return;

            var actionsComp = EnsureComp<ActionsComponent>(uid);
            foreach (var actionId in hulk.SuppressedActions.ToArray())
            {
                if (!TryComp<ActionComponent>(actionId, out var action))
                    continue;

                if (action.AttachedEntity == uid)
                    continue;

                if (action.Container is {} container && Exists(container))
                    _actionsSystem.AddAction((uid, actionsComp), (actionId, action), (container, (ActionsContainerComponent?) null));
                else
                    _actionsSystem.AddActionDirect((uid, actionsComp), (actionId, action));
            }

            hulk.SuppressedActions.Clear();
        }

        private void EnsureHulkScale(EntityUid uid, HulkGeneComponent hulk)
        {
            var desiredScale = new Vector2(1.5f, 1.5f);
            var currentScale = _scaleVisuals.GetSpriteScale(uid);

            if (currentScale == desiredScale)
                return;

            _scaleVisuals.SetSpriteScale(uid, desiredScale);

            if (Math.Abs(hulk.OriginalScale.X) <= float.Epsilon)
                return;

            var desiredFixtureScale = (desiredScale.X / hulk.OriginalScale.X) * 0.9f;
            var ratio = desiredFixtureScale / hulk.FixtureScaleFactor;

            if (Math.Abs(ratio - 1f) > float.Epsilon)
            {
                _physics.ScaleFixtures(uid, ratio);
                hulk.FixtureScaleFactor = desiredFixtureScale;
            }
        }

        private void ApplyMidget(EntityUid uid, MidgetGeneComponent midget)
        {
            if (midget.Applied)
                return;

            midget.OriginalScale = _scaleVisuals.GetSpriteScale(uid);
            var newScale = new Vector2(0.8f, 0.8f);
            _scaleVisuals.SetSpriteScale(uid, newScale);

            var scaleFactor = 1f;
            if (Math.Abs(midget.OriginalScale.X) > float.Epsilon)
                scaleFactor = newScale.X / midget.OriginalScale.X;

            midget.FixtureScaleFactor = scaleFactor;
            if (Math.Abs(scaleFactor - 1f) > float.Epsilon)
                _physics.ScaleFixtures(uid, scaleFactor);

            if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
            {
                var fixture = fixtures.Fixtures.First();
                midget.OriginalCollisionMask = fixture.Value.CollisionMask;
                midget.OriginalCollisionLayer = fixture.Value.CollisionLayer;

                var midgetMask = (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable);
                _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, midgetMask, fixtures);
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobLayer, fixtures);
            }

            midget.Applied = true;
        }

        private void RevertMidget(EntityUid uid, MidgetGeneComponent midget)
        {
            if (!midget.Applied)
                return;

            _scaleVisuals.SetSpriteScale(uid, midget.OriginalScale);

            if (Math.Abs(midget.FixtureScaleFactor - 1f) > float.Epsilon &&
                Math.Abs(midget.FixtureScaleFactor) > float.Epsilon)
            {
                _physics.ScaleFixtures(uid, 1f / midget.FixtureScaleFactor);
            }

            if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
            {
                var fixture = fixtures.Fixtures.First();
                if (midget.OriginalCollisionMask.HasValue)
                    _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, midget.OriginalCollisionMask.Value, fixtures);
                if (midget.OriginalCollisionLayer.HasValue)
                    _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, midget.OriginalCollisionLayer.Value, fixtures);
            }

            midget.FixtureScaleFactor = 1f;
            midget.Applied = false;
        }

        private void RemoveProvidedItemActions(EntityUid uid)
        {
            if (!TryComp<HandsComponent>(uid, out var hands) || !TryComp<InventoryComponent>(uid, out var inventory))
                return;

            foreach (var item in _inventorySystem.GetHandOrInventoryEntities((uid, hands, inventory)))
            {
                _actionsSystem.RemoveProvidedActions(uid, item);
            }
        }

        private void RestoreProvidedItemActions(EntityUid uid)
        {
            if (!TryComp<HandsComponent>(uid, out var hands) || !TryComp<InventoryComponent>(uid, out var inventory))
                return;

            foreach (var item in _inventorySystem.GetHandOrInventoryEntities((uid, hands, inventory)))
            {
                SlotFlags? slotFlags = null;
                if (_inventorySystem.TryGetContainingSlot((item, null, null), out var slotDef))
                    slotFlags = slotDef.SlotFlags;

                var ev = new GetItemActionsEvent(_actionContainer, uid, item, slotFlags);
                RaiseLocalEvent(item, ev);

                if (ev.Actions.Count > 0)
                    _actionsSystem.GrantActions((uid, (ActionsComponent?) null), ev.Actions, (item, (ActionsContainerComponent?) null));
            }
        }

        private void RemoveGene(EntityUid uid, GeneticsComponent component, string geneId)
        {
            foreach (var (blockIndex, mappedGene) in _geneMap)
            {
                if (mappedGene != geneId) continue;

                if (component.SeDna.TryGetValue(blockIndex, out var current))
                {
                    if (_geneMinThresholds.ContainsKey(geneId))
                    {
                        component.SeDna[blockIndex] = "000";
                        Dirty(uid, component);
                        continue;
                    }

                    if (_geneTargets.TryGetValue(geneId, out var target))
                    {
                        var invalidChar = target[0] == '0' ? '1' : '0';
                        component.SeDna[blockIndex] = invalidChar + current.Substring(1);
                        Dirty(uid, component);
                    }
                }
            }
        }

        private void OnDamageChanged(EntityUid uid, GeneticsComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased && args.DamageDelta != null)
            {
                var totalDelta = args.DamageDelta.GetTotal();
                if (totalDelta >= 100)
                {
                    
                    if (!TryComp<HulkGeneComponent>(uid, out var hulk) || !hulk.IsTransformed)
                    {
                        if (IsGeneActive("Hulk", component))
                        {
                            RemoveGene(uid, component, "Hulk");
                            _popupSystem.PopupEntity("The high impact destroys your Hulk DNA!", uid, uid, PopupType.LargeCaution);
                        }
                    }
                }
            }
        }

        private void OnMeleeHit(EntityUid uid, HulkGeneComponent component, ref MeleeHitEvent args)
        {
            if (!component.IsTransformed)
                return;

            float targetDamage;
            switch (component.Variant)
            {
                case "Godzilla": targetDamage = 30f; break;
                case "HonkChampion": targetDamage = 6f; break;
                default: targetDamage = 20f; break;
            }

            
            if (args.Weapon == args.User) 
            {
                var baseTotal = args.BaseDamage.GetTotal();
                var bonusNeeded = Math.Max(0, targetDamage - (float)baseTotal);
                args.BonusDamage.DamageDict.Add("Blunt", bonusNeeded);
            }
        }

        private void OnStrongMeleeHit(EntityUid uid, StrongGeneComponent component, ref MeleeHitEvent args)
        {
            
            if (args.Weapon != args.User)
                return;

            
            if (TryComp<HulkGeneComponent>(uid, out var hulk) && hulk.IsTransformed)
                return;

            const float bonus = 5f;
            args.BonusDamage.DamageDict.Add("Blunt", bonus);
        }

        private void OnActivateAttempt(EntityUid uid, ItemToggleComponent component, ref ItemToggleActivateAttemptEvent args)
        {
            if (args.User == null) return;
            
            
            if (MetaData(uid).EntityPrototype?.ID == "EnergySwordDouble")
            {
                if (TryComp<HulkGeneComponent>(args.User.Value, out var hulk) && hulk.IsTransformed)
                {
                    args.Cancelled = true;
                    args.Popup = "Your massive hands are too big to activate this delicate weapon!";
                }
            }
        }

        private bool IsGeneActive(string geneId, GeneticsComponent component)
        {
            if (component.InnateGenes.Contains(geneId))
            {
                return true;
            }

            foreach (var (blockIndex, mappedGene) in _geneMap)
            {
                if (mappedGene != geneId)
                    continue;

                if (component.SeDna.TryGetValue(blockIndex, out var hexVal))
                    return IsGeneActive(blockIndex, hexVal);
            }

            return false;
        }

        private void OnScannerContainerChanged(EntityUid uid, MedicalScannerComponent component, ContainerModifiedMessage args)
        {
            var consoles = EntityQueryEnumerator<GeneticsConsoleComponent>();
            while (consoles.MoveNext(out var consoleUid, out var consoleComp))
            {
                if (consoleComp.GeneticScanner == uid || ConsoleLinkedToScanner(consoleUid, uid))
                    UpdateUserInterface(consoleUid, consoleComp);
            }
        }

        private void RecheckScannerLink(EntityUid consoleUid, GeneticsConsoleComponent consoleComp)
        {
            if (!TryComp<DeviceLinkSourceComponent>(consoleUid, out var sourceComp))
            {
                consoleComp.GeneticScanner = null;
                return;
            }

            foreach (var port in sourceComp.Outputs.Values.SelectMany(outputs => outputs))
            {
                if (TryComp<MedicalScannerComponent>(port, out var scanner))
                {
                    consoleComp.GeneticScanner = port;
                    scanner.ConnectedConsole = consoleUid;
                    return;
                }
            }

            consoleComp.GeneticScanner = null;
        }

        private bool ConsoleLinkedToScanner(EntityUid consoleUid, EntityUid scannerUid)
        {
            if (!TryComp<DeviceLinkSourceComponent>(consoleUid, out var sourceComp))
                return false;

            foreach (var output in sourceComp.Outputs.Values)
            {
                if (output.Contains(scannerUid))
                    return true;
            }

            return false;
        }

        private void OnConsoleInit(EntityUid uid, GeneticsConsoleComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, SharedGeneticsConsole.BeakerSlotId, component.BeakerSlot);
            _itemSlotsSystem.AddItemSlot(uid, SharedGeneticsConsole.SyringeSlotId, component.SyringeSlot);
            EnsureTransferBuffers(component);
        }

        private void OnBeakerInserted(EntityUid uid, GeneticsConsoleComponent component, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != SharedGeneticsConsole.BeakerSlotId
                && args.Container.ID != SharedGeneticsConsole.SyringeSlotId)
                return;

            UpdateUserInterface(uid, component);
        }

        private void OnBeakerRemoved(EntityUid uid, GeneticsConsoleComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != SharedGeneticsConsole.BeakerSlotId
                && args.Container.ID != SharedGeneticsConsole.SyringeSlotId)
                return;

            UpdateUserInterface(uid, component);
        }

        private void OnInjectMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleInjectMessage args)
        {
            var beaker = _itemSlotsSystem.GetItemOrNull(uid, SharedGeneticsConsole.BeakerSlotId);
            if (beaker == null)
                return;

            if (!TryComp<SolutionComponent>(beaker.Value, out var beakerSoln))
                return;

            var beakerSolution = beakerSoln.Solution;

            if (component.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(component.GeneticScanner.Value, out var scanner))
                return;

            var body = scanner.BodyContainer.ContainedEntity;
            if (body == null)
                return;

            if (!TryComp<SolutionComponent>(body.Value, out var targetSoln))
                return;

            var transferAmount = FixedPoint2.Min(args.Amount, beakerSolution.Volume);
            if (transferAmount <= 0)
                return;

            var split = _solutionSystem.SplitSolution((beaker.Value, beakerSoln), transferAmount);
            _solutionSystem.TryAddSolution((body.Value, targetSoln), split);

            _audioSystem.PlayPvs("/Audio/Machines/machine_switch.ogg", uid);
            UpdateUserInterface(uid, component);
        }

        private void OnEjectBeakerMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleEjectBeakerMessage args)
        {
            _itemSlotsSystem.TryEject(uid, component.BeakerSlot, null, out _);
            UpdateUserInterface(uid, component);
        }

        private void OnToggleScannerLockMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleToggleScannerLockMessage args)
        {
            if (component.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(component.GeneticScanner.Value, out var scanner))
                return;

            scanner.Locked = args.Locked;
            UpdateUserInterface(uid, component);
        }

        private void OnStoreBufferMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleStoreBufferMessage args)
        {
            EnsureTransferBuffers(component);
            if (!TryGetSubjectGenetics(component, out var body, out var genetics, out _))
                return;

            if (args.BufferIndex < 0 || args.BufferIndex >= component.TransferBuffers.Count)
                return;

            var buffer = component.TransferBuffers[args.BufferIndex];
            buffer.SubjectName = Name(body);

            switch (args.Mode)
            {
                case GeneticsBufferCopyMode.UiOnly:
                    buffer.UiDna = new Dictionary<int, string>(genetics.UiDna);
                    buffer.SeDna = null;
                    break;
                case GeneticsBufferCopyMode.UiAndSe:
                    buffer.UiDna = new Dictionary<int, string>(genetics.UiDna);
                    buffer.SeDna = new Dictionary<int, string>(genetics.SeDna);
                    break;
                case GeneticsBufferCopyMode.SeOnly:
                    buffer.UiDna = null;
                    buffer.SeDna = new Dictionary<int, string>(genetics.SeDna);
                    break;
            }

            UpdateUserInterface(uid, component);
        }

        private void OnTransferBufferMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleTransferBufferMessage args)
        {
            EnsureTransferBuffers(component);
            if (args.BufferIndex < 0 || args.BufferIndex >= component.TransferBuffers.Count)
                return;

            var buffer = component.TransferBuffers[args.BufferIndex];
            if (!buffer.HasUi && !buffer.HasSe)
                return;

            TryLoadSyringeFromBufferSlot(uid, buffer);
            UpdateUserInterface(uid, component);
        }

        private void OnModifyGeneMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleModifyGeneMessage args)
        {
            if (component.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(component.GeneticScanner.Value, out var scanner))
                return;

            var body = scanner.BodyContainer.ContainedEntity;
            if (body == null)
                return;

            if (!TryComp<GeneticsComponent>(body.Value, out var genetics))
                return;

            if (!args.IsUiBlock)
                return;

            if (!genetics.UiDna.TryGetValue(args.BlockIndex, out var currentBlock))
                return;

            if (!TryUpdateGeneBlock(args.BlockIndex, args.SubIndex, currentBlock, out var updatedBlock))
                return;

            genetics.UiDna[args.BlockIndex] = updatedBlock;
            Dirty(body.Value, genetics);

            ApplyGeneticChanges(body.Value, genetics, args.BlockIndex);
            UpdateUserInterface(uid, component);
        }

        private bool TryGetSubjectGenetics(GeneticsConsoleComponent component, out EntityUid body, out GeneticsComponent genetics, out MedicalScannerComponent scanner)
        {
            body = default;
            genetics = default!;
            scanner = default!;

            if (component.GeneticScanner == null)
                return false;

            if (!TryComp<MedicalScannerComponent>(component.GeneticScanner.Value, out var scannerComp) || scannerComp == null)
                return false;
            scanner = scannerComp;

            var bodyEnt = scanner.BodyContainer.ContainedEntity;
            if (bodyEnt == null)
                return false;

            body = bodyEnt.Value;
            if (!TryComp<GeneticsComponent>(body, out var geneticsComp) || geneticsComp == null)
                return false;
            genetics = geneticsComp;

            return true;
        }

        private void ApplyBufferToSubject(EntityUid body, GeneticsComponent genetics, GeneticsTransferBuffer buffer, MedicalScannerComponent scanner)
        {
            ApplyGeneticPayload(body, genetics, buffer.UiDna, buffer.SeDna, scanner);
        }

        private void ApplyGeneticPayload(EntityUid body, GeneticsComponent genetics, Dictionary<int, string>? uiDna, Dictionary<int, string>? seDna, MedicalScannerComponent? scanner)
        {
            if (uiDna != null)
                genetics.UiDna = new Dictionary<int, string>(uiDna);

            if (seDna != null)
                genetics.SeDna = new Dictionary<int, string>(seDna);

            Dirty(body, genetics);

            var targetEntity = body;

            if (seDna != null)
            {
                if (scanner != null)
                {
                    ApplyStructuralChanges(body, genetics, SeHumanoidBlockIndex, scanner);
                    if (scanner.BodyContainer.ContainedEntity != null)
                        targetEntity = scanner.BodyContainer.ContainedEntity.Value;
                }
                else
                {
                    targetEntity = ApplyStructuralChangesWithoutScanner(body, genetics, SeHumanoidBlockIndex);
                }

                if (TryComp<GeneticsComponent>(targetEntity, out var seGenetics))
                {
                    UpdateInstability(targetEntity, seGenetics);
                    ApplyGenePassiveEffectsNow(targetEntity, seGenetics);
                }
            }

            if (uiDna != null && TryComp<GeneticsComponent>(targetEntity, out var uiGenetics))
            {
                for (var i = 0; i < UiBlockCount; i++)
                {
                    if (!uiGenetics.UiDna.ContainsKey(i))
                        continue;

                    ApplyGeneticChanges(targetEntity, uiGenetics, i);
                }
            }
        }

        public bool TryGrantGene(EntityUid uid, string? geneId)
        {
            if (string.IsNullOrWhiteSpace(geneId))
                return false;

            if (!TryComp<GeneticsComponent>(uid, out var genetics))
                return false;

            geneId = geneId.Trim();
            if (!_prototypeManager.HasIndex<GenePrototype>(geneId))
                return false;

            var blockIndex = -1;
            foreach (var (idx, id) in _geneMap)
            {
                if (id == geneId)
                {
                    blockIndex = idx;
                    break;
                }
            }

            if (blockIndex == -1 || !_geneTargets.TryGetValue(geneId, out var target))
                return false;

            var block = target.Length == 3 ? target : $"{target[0]}00";
            if (_geneMinThresholdHex.TryGetValue(geneId, out var minHex))
                block = minHex;
            genetics.SeDna[blockIndex] = block;
            Dirty(uid, genetics);

            var updated = ApplyStructuralChangesWithoutScanner(uid, genetics, SeHumanoidBlockIndex);
            if (TryComp<GeneticsComponent>(updated, out var updatedGenetics))
            {
                UpdateInstability(updated, updatedGenetics);
                ApplyGenePassiveEffectsNow(updated, updatedGenetics);
            }

            return true;
        }

        private EntityUid ApplyStructuralChangesWithoutScanner(EntityUid uid, GeneticsComponent component, int changedBlock)
        {
            if (changedBlock != SeHumanoidBlockIndex)
                return uid;

            if (!component.SeDna.TryGetValue(changedBlock, out var hexBlock))
                return uid;

            if (!int.TryParse(hexBlock, System.Globalization.NumberStyles.HexNumber, null, out var value))
                return uid;

            var targetForm = "Human";
            if (value >= 0x800)
            {
                var isUnathi = false;
                if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
                    isUnathi = appearance.Species == "Reptilian";

                targetForm = isUnathi ? "Kobold" : "Monkey";
            }
            if (!_prototypeManager.TryIndex<PolymorphPrototype>(targetForm, out var prototype))
                return uid;

            var newEntity = _polymorphSystem.PolymorphEntity(uid, prototype.ID);
            if (newEntity == null)
                return uid;

            if (TryComp<GeneticsComponent>(uid, out var sourceGenetics))
            {
                var targetGenetics = EnsureComp<GeneticsComponent>(newEntity.Value);
                targetGenetics.UiDna = new Dictionary<int, string>(sourceGenetics.UiDna);
                targetGenetics.SeDna = new Dictionary<int, string>(sourceGenetics.SeDna);
                targetGenetics.InnateGenes = new HashSet<string>(sourceGenetics.InnateGenes);
                targetGenetics.Instability = sourceGenetics.Instability;
                targetGenetics.RadiationExposure = sourceGenetics.RadiationExposure;
            }

            if (TryComp<DamageableComponent>(uid, out var sourceDamage) &&
                TryComp<DamageableComponent>(newEntity.Value, out var targetDamage))
            {
                var damageCopy = new DamageSpecifier(sourceDamage.Damage);
                _damageableSystem.SetDamage((newEntity.Value, targetDamage), damageCopy);
            }

            return newEntity.Value;
        }

        private void TryLoadSyringeFromBufferSlot(EntityUid consoleUid, GeneticsTransferBuffer buffer)
        {
            var syringe = _itemSlotsSystem.GetItemOrNull(consoleUid, SharedGeneticsConsole.SyringeSlotId);
            if (syringe == null)
                return;

            if (!TryComp<InjectorComponent>(syringe.Value, out _))
                return;

            var geneticSyringe = EnsureComp<GeneticSyringeComponent>(syringe.Value);
            geneticSyringe.SubjectName = buffer.SubjectName;
            geneticSyringe.UiDna = buffer.UiDna != null ? new Dictionary<int, string>(buffer.UiDna) : null;
            geneticSyringe.SeDna = buffer.SeDna != null ? new Dictionary<int, string>(buffer.SeDna) : null;

            if (_solutionSystem.TryGetSolution(syringe.Value, "injector", out var soln, out var solution) && solution.Volume <= 0)
                _solutionSystem.TryAddReagent(soln!.Value, "Water", FixedPoint2.New(15));
        }

        private void OnGeneticSyringeAfterInject(EntityUid uid, GeneticSyringeComponent component, AfterInjectEvent args)
        {
            if (!component.HasData)
                return;

            if (!TryComp<GeneticsComponent>(args.Target, out var genetics))
                return;

            ApplyGeneticPayload(args.Target, genetics, component.UiDna, component.SeDna, null);

            component.SubjectName = string.Empty;
            component.UiDna = null;
            component.SeDna = null;
        }

        private bool TryUpdateGeneBlock(int blockIndex, int subIndex, string currentBlock, out string updatedBlock)
        {
            updatedBlock = currentBlock;
            if (blockIndex < 0 || blockIndex >= UiBlockCount)
                return false;

            var targetBlock = currentBlock.PadRight(3, '0');

            if (blockIndex != 12 && blockIndex != 31)
                subIndex = 0;

            if (!TryHandleIndexedBlocks(blockIndex, subIndex, targetBlock, out updatedBlock))
                return false;

            return true;
        }

        private bool TryHandleIndexedBlocks(int blockIndex, int subIndex, string currentBlock, out string updatedBlock)
        {
            updatedBlock = currentBlock;

            if (blockIndex == 12)
            {
                var skinTone = int.Parse(currentBlock, System.Globalization.NumberStyles.HexNumber);
                skinTone = (skinTone + 1) % 0xDC; 
                updatedBlock = skinTone.ToString("X3");
                return true;
            }

            if (blockIndex == 31)
            {
                var genderValue = int.Parse(currentBlock, System.Globalization.NumberStyles.HexNumber);
                genderValue = genderValue switch
                {
                    >= 0x801 => 0x23D,
                    >= 0x23E => 0x801,
                    _ => 0x23E
                };
                updatedBlock = genderValue.ToString("X3");
                return true;
            }

            return TryUpdateBySubIndex(blockIndex, 0, currentBlock, out updatedBlock);
        }

        private bool TryUpdateBySubIndex(int blockIndex, int subIndex, string currentBlock, out string updatedBlock)
        {
            updatedBlock = currentBlock;
            if (subIndex < 0 || subIndex > 2)
                return false;

            var updated = currentBlock.ToCharArray();
            updated[subIndex] = NextGeneValue(blockIndex, subIndex, currentBlock);
            updatedBlock = new string(updated);
            return true;
        }

        private char NextGeneValue(int blockIndex, int subIndex, string currentBlock)
        {
            var currentChar = currentBlock[subIndex];
            var currentValue = int.Parse(currentChar.ToString(), System.Globalization.NumberStyles.HexNumber);

            var range = (min: 0, max: 15);
            var nextValue = currentValue + 1;
            if (nextValue > range.max)
                nextValue = range.min;

            return nextValue.ToString("X")[0];
        }
    }
}

