// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration;
using Content.Shared.ActionBlocker;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Systems;
using Content.Shared.Genetics.UI;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Server.Genetics.System
{
    [UsedImplicitly]
    public sealed class DnaModifierConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly DnaClientSystem _dnaClient = default!;
        [Dependency] private readonly DnaModifierSystem _dnaModifier = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private static readonly EntProtoId Injector = "DnaInjector";
        private static readonly ProtoId<DamageTypePrototype> RadDamage = "Radiation";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DnaModifierConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DnaModifierConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
            SubscribeLocalEvent<DnaModifierConsoleComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<DnaModifierConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DnaModifierConsoleComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<DnaModifierConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<DnaModifierConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);

            SubscribeNetworkEvent<DnaModifierUpdateEvent>(OnUpdateUI);
            SubscribeNetworkEvent<DnaModifierConsoleEjectEvent>(OnEjectPressed);
            SubscribeNetworkEvent<DnaModifierConsoleEjectRejuveEvent>(OnEjectRejuvePressed);
            SubscribeNetworkEvent<DnaModifierConsoleReagentButtonEvent>(OnReagentButtonPressed);

            SubscribeNetworkEvent<DnaModifierConsoleSaveServerEvent>(OnSaveServerPressed);
            SubscribeNetworkEvent<DnaModifierConsoleClearBufferEvent>(OnClearBufferPressed);
            SubscribeNetworkEvent<DnaModifierConsoleRenameBufferEvent>(OnRenameBufferPressed);
            SubscribeNetworkEvent<DnaModifierConsoleInjectorEvent>(OnInjectorPressed);
            SubscribeNetworkEvent<DnaModifierInjectBlockEvent>(OnInjectBlockPressed);
            SubscribeNetworkEvent<DnaModifierConsoleSubjectInjectEvent>(OnSubjectInjectPressed);

            SubscribeNetworkEvent<DnaModifierConsoleExportOnDiskEvent>(OnExportOnDiskPressed);
            SubscribeNetworkEvent<DnaModifierConsoleExportFromDiskEvent>(OnExportFromDiskPressed);
            SubscribeNetworkEvent<DnaModifierConsoleClearDiskEvent>(OnClearDiskPressed);

            SubscribeNetworkEvent<DnaModifierConsoleReleverationEvent>(OnReleverationPressed);
            SubscribeNetworkEvent<DnaModifierConsoleReleverationsEvent>(OnReleverationsPressed);
        }

        private bool IsAuthorized(NetEntity netConsole, EntitySessionEventArgs session, out EntityUid user)
        {
            user = default;

            if (session.SenderSession.AttachedEntity is not { } attached)
                return false;

            var uid = GetEntity(netConsole);
            if (!HasComp<DnaModifierConsoleComponent>(uid))
                return false;

            if (!_uiSystem.IsUiOpen(uid, DnaModifierUiKey.Key, attached))
                return false;

            if (!_actionBlocker.CanInteract(attached, uid))
                return false;

            if (!_powerReceiverSystem.IsPowered(uid))
                return false;

            user = attached;
            return true;
        }

        #region UI logic
        private void OnInit(EntityUid uid, DnaModifierConsoleComponent component, ComponentInit args)
        {
            component.LastInjectorTime = _timing.CurTime + component.InjectorCooldown;
            component.LastSubjectInjectTime = _timing.CurTime + component.SubjectInjectCooldown;
            _signalSystem.EnsureSourcePorts(uid, DnaModifierConsoleComponent.ScannerPort);

            Dirty(uid, component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<DnaModifierConsoleComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var component, out var transform))
            {
                if (component.NextUpdate > _timing.CurTime)
                    continue;

                component.NextUpdate = _timing.CurTime + component.UpdateInterval;
                if (component.GeneticScanner != null && HasComp<MedicalScannerComponent>(component.GeneticScanner))
                {
                    Transform(component.GeneticScanner.Value).Coordinates.TryDistance(EntityManager, transform.Coordinates, out float scannerDistance);
                    component.GeneticScannerInRange = scannerDistance <= component.MaxDistance;

                    UpdateUserInterface(uid, component);
                }
            }
        }

        private void OnUpdateUI(DnaModifierUpdateEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            if (!TryComp<DnaModifierConsoleComponent>(GetEntity(args.Uid), out var component))
                return;

            UpdateUserInterface(GetEntity(args.Uid), component);
        }

        private void OnPowerChanged(EntityUid uid, DnaModifierConsoleComponent component, ref PowerChangedEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnMapInit(EntityUid uid, DnaModifierConsoleComponent component, MapInitEvent args)
        {
            if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
                return;

            foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
            {
                if (TryComp<MedicalScannerComponent>(port, out var scanner))
                {
                    component.GeneticScanner = port;
                    scanner.ConnectedConsole = uid;
                }
            }
        }

        private void OnNewLink(EntityUid uid, DnaModifierConsoleComponent component, NewLinkEvent args)
        {
            if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner) && args.SourcePort == DnaModifierConsoleComponent.ScannerPort)
            {
                component.GeneticScanner = args.Sink;
                scanner.ConnectedConsole = uid;
            }

            RecheckConnections(uid, component.GeneticScanner, component);
        }

        private void OnPortDisconnected(EntityUid uid, DnaModifierConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port == DnaModifierConsoleComponent.ScannerPort)
                component.GeneticScanner = null;

            UpdateUserInterface(uid, component);
        }

        private void OnUIOpen(EntityUid uid, DnaModifierConsoleComponent component, AfterActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnAnchorChanged(EntityUid uid, DnaModifierConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                RecheckConnections(uid, component.GeneticScanner, component);
                return;
            }
            UpdateUserInterface(uid, component);
        }

        public void UpdateUserInterface(EntityUid consoleUid, DnaModifierConsoleComponent consoleComponent)
        {
            if (!_uiSystem.HasUi(consoleUid, DnaModifierUiKey.Key))
                return;

            if (!_powerReceiverSystem.IsPowered(consoleUid))
            {
                _uiSystem.CloseUis(consoleUid);
                return;
            }

            var newState = GetUserInterfaceState((consoleUid, consoleComponent));
            _uiSystem.SetUiState(consoleUid, DnaModifierUiKey.Key, newState);
        }

        public void RecheckConnections(EntityUid console, EntityUid? scanner, DnaModifierConsoleComponent? consoleComp = null)
        {
            if (!Resolve(console, ref consoleComp))
                return;

            if (scanner != null)
            {
                Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform((console)).Coordinates, out float scannerDistance);
                consoleComp.GeneticScannerInRange = scannerDistance <= consoleComp.MaxDistance;
            }

            UpdateUserInterface(console, consoleComp);
        }

        private DnaModifierBoundUserInterfaceState GetUserInterfaceState(Entity<DnaModifierConsoleComponent> ent)
        {
            // genetic scanner info
            EntityUid? inputContainer = null;
            NetEntity console = GetNetEntity(ent);

            string scanBodyInfo = string.Empty;
            string scannerBodyStatus = string.Empty;
            string scannerBodyDna = string.Empty;
            string scannerSpecies = string.Empty;

            float scannerBodyHealth = -1;
            float scannerBodyRadiation = 0;

            bool hasDisk = false;
            bool scannerHasBeaker = false;
            bool scannerInRange = ent.Comp.GeneticScannerInRange;

            EnzymeInfo? enzyme = null;
            UniqueIdentifiersData? uniqueIdentifiers = null;
            List<EnzymesPrototypeInfo>? enzymesPrototypes = null;

            var currentTime = _timing.CurTime;
            var injectorCooldown = ent.Comp.LastInjectorTime + ent.Comp.InjectorCooldown;
            var subjectInjectCooldown = ent.Comp.LastSubjectInjectTime + ent.Comp.SubjectInjectCooldown;

            var buffer = GetAllBuffers(ent);
            if (ent.Comp.GeneticScanner != null && TryComp<MedicalScannerComponent>(ent.Comp.GeneticScanner, out var scanner))
            {
                EntityUid? scanBody = scanner.BodyContainer.ContainedEntity;
                inputContainer = _itemSlotsSystem.GetItemOrNull(ent.Comp.GeneticScanner.Value, SharedDnaModifier.InputSlotName);

                if (_itemSlotsSystem.TryGetSlot(ent, SharedDnaModifier.DiskSlotName, out var diskSlot)
                    && diskSlot.HasItem && diskSlot.Item != null)
                {
                    hasDisk = true;
                    if (_dnaModifier.TryGetDataFromDisk(diskSlot.Item.Value, out var data))
                        enzyme = data;
                }

                if (_itemSlotsSystem.TryGetSlot(ent.Comp.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var beakerSlot)
                    && beakerSlot.HasItem)
                {
                    scannerHasBeaker = true;
                }

                // GET STATE
                if (scanBody != null && TryComp<MobStateComponent>(scanBody, out var mobState))
                {
                    scanBodyInfo = MetaData(scanBody.Value).EntityName;
                    scannerBodyStatus = (mobState.CurrentState != MobState.Invalid)
                        ? GetStatus(mobState.CurrentState)
                        : Loc.GetString("dna-modifier-entity-unknown-text");

                    if (TryComp<HumanoidAppearanceComponent>(scanBody.Value, out var humanoid))
                    {
                        if (_prototypeManager.TryIndex(humanoid.Species, out SpeciesPrototype? speciesProto))
                        {
                            scannerSpecies = Loc.GetString(speciesProto.Name);
                        }
                        else
                        {
                            scannerSpecies = humanoid.Species.ToString();
                        }
                    }

                    if (TryComp<DnaComponent>(scanBody.Value, out var dna))
                        scannerBodyDna = dna.DNA ?? string.Empty;

                    if (TryComp<DamageableComponent>(scanBody.Value, out var damage))
                    {
                        if (TryComp<MobThresholdsComponent>(scanBody.Value, out var mobThresholds))
                        {
                            FixedPoint2 deathHealth = FixedPoint2.Zero;
                            foreach (var threshold in mobThresholds.Thresholds)
                            {
                                if (threshold.Value == MobState.Dead)
                                {
                                    deathHealth = threshold.Key;
                                    break;
                                }
                            }

                            if (deathHealth > FixedPoint2.Zero)
                            {
                                float currentHealth = 1.0f - (damage.TotalDamage.Float() / deathHealth.Float());
                                scannerBodyHealth = Math.Clamp(currentHealth, 0f, 1f);
                            }
                        }

                        if (damage.Damage.DamageDict.TryGetValue(RadDamage, out var radiationDamage))
                            scannerBodyRadiation = Math.Clamp(radiationDamage.Float() / 200f, 0f, 1f);
                    }

                    if (TryComp<DnaModifierComponent>(scanBody.Value, out var dnaModifier))
                    {
                        uniqueIdentifiers = dnaModifier.UniqueIdentifiers;
                        enzymesPrototypes = dnaModifier.EnzymesPrototypes;
                    }
                }
            }

            return new DnaModifierBoundUserInterfaceState(
                console,
                uniqueIdentifiers,
                enzymesPrototypes,
                enzyme,
                scanBodyInfo,
                scannerBodyStatus,
                scannerBodyDna,
                scannerSpecies,
                scannerBodyHealth,
                scannerBodyRadiation,
                scannerHasBeaker,
                BuildInputContainerInfo(inputContainer),
                scannerInRange,
                hasDisk,
                buffer,
                currentTime < injectorCooldown ? injectorCooldown - currentTime : TimeSpan.Zero,
                currentTime < subjectInjectCooldown ? subjectInjectCooldown - currentTime : TimeSpan.Zero
            );
        }

        private string GetStatus(MobState mobState)
        {
            return mobState switch
            {
                MobState.Alive => Loc.GetString("dna-modifier-entity-alive-text"),
                MobState.Critical => Loc.GetString("dna-modifier-entity-critical-text"),
                MobState.Dead => Loc.GetString("dna-modifier-entity-dead-text"),
                _ => Loc.GetString("dna-modifier-entity-unknown-text"),
            };
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }

        public Dictionary<int, EnzymeInfo?> GetAllBuffers(EntityUid uid)
        {
            var buffers = new Dictionary<int, EnzymeInfo?>();
            if (!TryComp<DnaClientComponent>(uid, out var client))
                return buffers;

            for (int i = 1; i <= 3; i++)
            {
                if (_dnaClient.TryGetBufferData((uid, client), i, out var data))
                    buffers[i] = data;
            }

            return buffers;
        }
        #endregion

        #region Console logic
        private void PlayClickSound(Entity<DnaModifierConsoleComponent> ent)
            => _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2f));

        private void OnEjectPressed(DnaModifierConsoleEjectEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            if (!TryComp<DnaModifierConsoleComponent>(GetEntity(args.Uid), out var console) || console.GeneticScanner == null)
                return;

            if (!HasComp<MedicalScannerComponent>(console.GeneticScanner))
                return;

            if (_container.TryGetContainer(console.GeneticScanner.Value, SharedDnaModifier.OccupantSlotName, out var container))
                _container.EmptyContainer(container);

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(GetEntity(args.Uid), console);
        }

        private void OnEjectRejuvePressed(DnaModifierConsoleEjectRejuveEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            if (!TryComp<DnaModifierConsoleComponent>(GetEntity(args.Uid), out var console) || console.GeneticScanner == null)
                return;

            if (!HasComp<MedicalScannerComponent>(console.GeneticScanner))
                return;

            if (_itemSlotsSystem.TryGetSlot(console.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var slot))
                _itemSlotsSystem.TryEject(console.GeneticScanner.Value, slot, null, out var _, true);

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(GetEntity(args.Uid), console);
        }

        private void OnReagentButtonPressed(DnaModifierConsoleReagentButtonEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            if (!TryComp<DnaModifierConsoleComponent>(GetEntity(args.Uid), out var console) || console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner) || scanner.BodyContainer.ContainedEntity == null
                || !_itemSlotsSystem.TryGetSlot(console.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var slot))
                return;

            if (slot.Item == null || !HasComp<SolutionContainerManagerComponent>(slot.Item.Value)
                || !_solutionContainerSystem.TryGetSolution(slot.Item.Value, SharedDnaModifier.SolutionSlotName, out var sourceSolution, out var sourceSolutionComp))
                return;

            var targetEntity = scanner.BodyContainer.ContainedEntity.Value;
            if (!HasComp<SolutionContainerManagerComponent>(targetEntity)
                || !_solutionContainerSystem.TryGetInjectableSolution(targetEntity, out var targetSolution, out _))
                return;

            FixedPoint2 transferAmount = args.Amount switch
            {
                DnaModifierReagentAmount.U1 => FixedPoint2.New(1),
                DnaModifierReagentAmount.U5 => FixedPoint2.New(5),
                DnaModifierReagentAmount.U10 => FixedPoint2.New(10),
                DnaModifierReagentAmount.U25 => FixedPoint2.New(25),
                DnaModifierReagentAmount.U50 => FixedPoint2.New(50),
                DnaModifierReagentAmount.U100 => FixedPoint2.New(100),
                DnaModifierReagentAmount.All => sourceSolutionComp.GetReagentQuantity(args.ReagentId),
                _ => FixedPoint2.Zero
            };

            if (transferAmount <= FixedPoint2.Zero || sourceSolutionComp.GetReagentQuantity(args.ReagentId) < transferAmount)
                return;

            var reagentSolution = new Solution();
            reagentSolution.AddReagent(args.ReagentId, transferAmount);

            _solutionContainerSystem.RemoveReagent(sourceSolution.Value, args.ReagentId, transferAmount);
            if (!_solutionContainerSystem.TryAddSolution(targetSolution.Value, reagentSolution))
                return;

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(GetEntity(args.Uid), console);
        }

        private void OnSaveServerPressed(DnaModifierConsoleSaveServerEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null
                || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier))
                return;

            EnzymeInfo? dataToSend = null;
            switch (args.CurrentType)
            {
                case 1:
                    if (dnaModifier.UniqueIdentifiers != null)
                    {
                        dataToSend = new EnzymeInfo()
                        {
                            Identifier = _dnaModifier.CloneUniqueIdentifiers(dnaModifier.UniqueIdentifiers),
                            Info = null
                        };
                    }
                    break;

                case 2:
                    if (dnaModifier.UniqueIdentifiers != null && dnaModifier.EnzymesPrototypes != null)
                    {
                        dataToSend = new EnzymeInfo()
                        {
                            Identifier = _dnaModifier.CloneUniqueIdentifiers(dnaModifier.UniqueIdentifiers),
                            Info = _dnaModifier.CloneEnzymesPrototypes(dnaModifier.EnzymesPrototypes)
                        };
                    }
                    break;

                case 3:
                    if (dnaModifier.EnzymesPrototypes != null)
                    {
                        dataToSend = new EnzymeInfo()
                        {
                            Identifier = null,
                            Info = _dnaModifier.CloneEnzymesPrototypes(dnaModifier.EnzymesPrototypes)
                        };
                    }
                    break;

                default: return;
            }

            if (dataToSend == null)
                return;

            _dnaClient.TryAddToBuffer((clientEntity, client), args.CurrentSection, dataToSend);

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(clientEntity, console);
        }

        private void OnClearBufferPressed(DnaModifierConsoleClearBufferEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            _dnaClient.TryClearBuffer((clientEntity, client), args.Index);

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(clientEntity, console);
        }

        private void OnRenameBufferPressed(DnaModifierConsoleRenameBufferEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Console, session, out var user))
                return;

            var clientEntity = GetEntity(args.Console);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((clientEntity, client), args.Index, out var data))
                return;

            if (!TryComp<ActorComponent>(user, out var playerActor))
                return;

            var playerSession = playerActor.PlayerSession;
            _quickDialog.OpenDialog(playerSession, Loc.GetString("dna-modifier-button-rename"), "",
                (string name) =>
                {
                    var finalName = string.IsNullOrWhiteSpace(name)
                        ? data.SampleName
                        : name;

                    if (!IsAuthorized(args.Console, session, out _))
                        return;

                    _dnaClient.TryRenameBuffer((clientEntity, client), args.Index, finalName);

                    PlayClickSound((clientEntity, console));
                    UpdateUserInterface(clientEntity, console);
                });
        }

        private void OnInjectorPressed(DnaModifierConsoleInjectorEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null
                || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((clientEntity, client), args.Index, out var data))
                return;

            _dnaModifier.OnFillingInjector(_entManager.SpawnEntity(Injector, Transform(clientEntity).Coordinates),
                data.Identifier, data.Info);

            console.LastInjectorTime = _timing.CurTime;

            PlayClickSound((clientEntity, console));
            UpdateUserInterface(clientEntity, console);
        }

        private void OnInjectBlockPressed(DnaModifierInjectBlockEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null
                || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((clientEntity, client), args.Index, out var data) || data.Info == null)
                return;

            var targetBlock = data.Info.FirstOrDefault(e => e.Order == args.CurrentBlock);
            if (targetBlock == null)
                return;

            var singleBlockInfo = new List<EnzymesPrototypeInfo> { targetBlock };
            _dnaModifier.OnFillingInjector(_entManager.SpawnEntity(Injector, Transform(clientEntity).Coordinates),
                null, singleBlockInfo);

            console.LastInjectorTime = _timing.CurTime;

            PlayClickSound((clientEntity, console));
            UpdateUserInterface(clientEntity, console);
        }

        private void OnSubjectInjectPressed(DnaModifierConsoleSubjectInjectEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null
                || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier) || !scanBody.HasValue)
                return;

            if (!_dnaClient.TryGetBufferData((clientEntity, client), args.Index, out var data))
                return;

            PlayClickSound((clientEntity, console));
            _dnaModifier.ChangeDna((scanBody.Value, dnaModifier), data);

            console.LastSubjectInjectTime = _timing.CurTime;

            var damage = new DamageSpecifier { DamageDict = { { RadDamage, 20 } } };
            _damage.TryChangeDamage(scanBody.Value, damage, true);
        }

        private void OnExportOnDiskPressed(DnaModifierConsoleExportOnDiskEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null
                || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((clientEntity, client), args.Index, out var data))
                return;

            if (_itemSlotsSystem.TryGetSlot(clientEntity, SharedDnaModifier.DiskSlotName, out var diskSlot) && diskSlot.Item != null)
            {
                _dnaModifier.TrySaveInDisk(diskSlot.Item.Value, data);

                PlayClickSound((clientEntity, console));
                UpdateUserInterface(clientEntity, console);
            }
        }

        private void OnExportFromDiskPressed(DnaModifierConsoleExportFromDiskEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null
                || !TryComp<DnaClientComponent>(clientEntity, out var client))
                return;

            if (_itemSlotsSystem.TryGetSlot(clientEntity, SharedDnaModifier.DiskSlotName, out var diskSlot) && diskSlot.Item != null)
            {
                _dnaModifier.TryGetDataFromDisk(diskSlot.Item.Value, out var data);
                if (data == null)
                    return;

                _dnaClient.TryAddToBufferDisk((clientEntity, client), args.Index, data);

                PlayClickSound((clientEntity, console));
                UpdateUserInterface(clientEntity, console);
            }
        }

        private void OnClearDiskPressed(DnaModifierConsoleClearDiskEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            var clientEntity = GetEntity(args.Uid);
            if (!TryComp<DnaModifierConsoleComponent>(clientEntity, out var console) || console.GeneticScanner == null)
                return;

            if (_itemSlotsSystem.TryGetSlot(clientEntity, SharedDnaModifier.DiskSlotName, out var diskSlot) && diskSlot.Item != null)
            {
                PlayClickSound((clientEntity, console));
                _dnaModifier.TryClearDiskData(diskSlot.Item.Value);
            }
        }

        private void OnReleverationPressed(DnaModifierConsoleReleverationEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            if (!TryComp<DnaModifierConsoleComponent>(GetEntity(args.Uid), out var console) || console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier) || !scanBody.HasValue)
                return;

            if (args.CurrentTab == 0 && dnaModifier.UniqueIdentifiers != null)
            {
                ModifyUniqueIdentifiers(dnaModifier.UniqueIdentifiers, args.CurrentBlock, args.CurrentValue, args.Intensity);
            }
            else if (args.CurrentTab == 1 && dnaModifier.EnzymesPrototypes != null)
            {
                ModifyEnzymesPrototypes(dnaModifier.EnzymesPrototypes, args.CurrentBlock, args.CurrentValue, args.Intensity);
            }

            AddRadiationDamage(scanBody.Value, args.Intensity);
            Dirty(scanBody.Value, dnaModifier);

            _dnaModifier.ChangeDna((scanBody.Value, dnaModifier), args.CurrentTab);

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(GetEntity(args.Uid), console);
        }

        private void OnReleverationsPressed(DnaModifierConsoleReleverationsEvent args, EntitySessionEventArgs session)
        {
            if (!IsAuthorized(args.Uid, session, out _))
                return;

            if (!TryComp<DnaModifierConsoleComponent>(GetEntity(args.Uid), out var console) || console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier) || !scanBody.HasValue)
                return;

            int type = -1;
            if (args.CurrentTab == 0 && dnaModifier.UniqueIdentifiers != null)
            {
                type = 0;
                ModifyUniqueIdentifiers(dnaModifier.UniqueIdentifiers, args.Intensity, args.Duration);
            }
            else if (args.CurrentTab == 1 && dnaModifier.EnzymesPrototypes != null)
            {
                type = 1;
                ModifyEnzymesPrototypes(dnaModifier.EnzymesPrototypes, args.Intensity, args.Duration);
            }

            AddRadiationDamage(scanBody.Value, args.Intensity);
            Dirty(scanBody.Value, dnaModifier);

            _dnaModifier.ChangeDna((scanBody.Value, dnaModifier), type);

            PlayClickSound((GetEntity(args.Uid), console));
            UpdateUserInterface(GetEntity(args.Uid), console);
        }

        private void ModifyUniqueIdentifiers(UniqueIdentifiersData uniqueIdentifiers, string block, int value, float intensity)
        {
            var fields = new List<(string[] Field, string Name)>
            {
                (uniqueIdentifiers.HairColorR, nameof(uniqueIdentifiers.HairColorR)),
                (uniqueIdentifiers.HairColorG, nameof(uniqueIdentifiers.HairColorG)),
                (uniqueIdentifiers.HairColorB, nameof(uniqueIdentifiers.HairColorB)),
                (uniqueIdentifiers.SecondaryHairColorR, nameof(uniqueIdentifiers.SecondaryHairColorR)),
                (uniqueIdentifiers.SecondaryHairColorG, nameof(uniqueIdentifiers.SecondaryHairColorG)),
                (uniqueIdentifiers.SecondaryHairColorB, nameof(uniqueIdentifiers.SecondaryHairColorB)),
                (uniqueIdentifiers.BeardColorR, nameof(uniqueIdentifiers.BeardColorR)),
                (uniqueIdentifiers.BeardColorG, nameof(uniqueIdentifiers.BeardColorG)),
                (uniqueIdentifiers.BeardColorB, nameof(uniqueIdentifiers.BeardColorB)),
                (uniqueIdentifiers.SkinTone, nameof(uniqueIdentifiers.SkinTone)),
                (uniqueIdentifiers.FurColorR, nameof(uniqueIdentifiers.FurColorR)),
                (uniqueIdentifiers.FurColorG, nameof(uniqueIdentifiers.FurColorG)),
                (uniqueIdentifiers.FurColorB, nameof(uniqueIdentifiers.FurColorB)),
                (uniqueIdentifiers.HeadAccessoryColorR, nameof(uniqueIdentifiers.HeadAccessoryColorR)),
                (uniqueIdentifiers.HeadAccessoryColorG, nameof(uniqueIdentifiers.HeadAccessoryColorG)),
                (uniqueIdentifiers.HeadAccessoryColorB, nameof(uniqueIdentifiers.HeadAccessoryColorB)),
                (uniqueIdentifiers.HeadMarkingColorR, nameof(uniqueIdentifiers.HeadMarkingColorR)),
                (uniqueIdentifiers.HeadMarkingColorG, nameof(uniqueIdentifiers.HeadMarkingColorG)),
                (uniqueIdentifiers.HeadMarkingColorB, nameof(uniqueIdentifiers.HeadMarkingColorB)),
                (uniqueIdentifiers.BodyMarkingColorR, nameof(uniqueIdentifiers.BodyMarkingColorR)),
                (uniqueIdentifiers.BodyMarkingColorG, nameof(uniqueIdentifiers.BodyMarkingColorG)),
                (uniqueIdentifiers.BodyMarkingColorB, nameof(uniqueIdentifiers.BodyMarkingColorB)),
                (uniqueIdentifiers.TailMarkingColorR, nameof(uniqueIdentifiers.TailMarkingColorR)),
                (uniqueIdentifiers.TailMarkingColorG, nameof(uniqueIdentifiers.TailMarkingColorG)),
                (uniqueIdentifiers.TailMarkingColorB, nameof(uniqueIdentifiers.TailMarkingColorB)),
                (uniqueIdentifiers.EyeColorR, nameof(uniqueIdentifiers.EyeColorR)),
                (uniqueIdentifiers.EyeColorG, nameof(uniqueIdentifiers.EyeColorG)),
                (uniqueIdentifiers.EyeColorB, nameof(uniqueIdentifiers.EyeColorB)),
                (uniqueIdentifiers.Gender, nameof(uniqueIdentifiers.Gender)),
                (uniqueIdentifiers.BeardStyle, nameof(uniqueIdentifiers.BeardStyle)),
                (uniqueIdentifiers.HairStyle, nameof(uniqueIdentifiers.HairStyle)),
                (uniqueIdentifiers.HeadAccessoryStyle, nameof(uniqueIdentifiers.HeadAccessoryStyle)),
                (uniqueIdentifiers.HeadMarkingStyle, nameof(uniqueIdentifiers.HeadMarkingStyle)),
                (uniqueIdentifiers.BodyMarkingStyle, nameof(uniqueIdentifiers.BodyMarkingStyle)),
                (uniqueIdentifiers.TailMarkingStyle, nameof(uniqueIdentifiers.TailMarkingStyle))
            };

            if (_random.NextFloat() < 0.025f)
            {
                var randomField = fields[_random.Next(fields.Count)];
                int randomIndex = _random.Next(0, randomField.Field.Length);
                if (randomField.Name == nameof(uniqueIdentifiers.SkinTone))
                {
                    randomField.Field[randomIndex] = GenerateSkinToneComponent(randomIndex, intensity, 1.0f);
                }
                else
                {
                    randomField.Field[randomIndex] = GenerateRandomHexValue(randomField.Field[randomIndex], intensity, 1.0f);
                }
                return;
            }

            if (!int.TryParse(block, out int blockNumber))
                return;

            var blockMap = new Dictionary<int, int>
            {
                { 1, 0 }, { 2, 1 }, { 3, 2 }, { 4, 3 }, { 5, 4 }, { 6, 5 }, { 7, 6 },
                { 8, 7 }, { 9, 8 }, { 13, 9 },
                { 14, 10 }, { 15, 11 }, { 16, 12 }, { 17, 13 }, { 18, 14 }, { 19, 15 },
                { 20, 16 }, { 21, 17 }, { 22, 18 }, { 23, 19 }, { 24, 20 }, { 25, 21 },
                { 26, 22 }, { 27, 23 }, { 28, 24 }, { 29, 25 }, { 30, 26 }, { 31, 27 },
                { 32, 28 }, { 33, 29 }, { 34, 30 }, { 35, 31 }, { 36, 32 }, { 37, 33 },
                { 38, 34 }
            };

            if (!blockMap.ContainsKey(blockNumber))
                return;

            int blockIndex = blockMap[blockNumber];
            if (blockIndex < 0 || blockIndex >= fields.Count)
                return;

            var field = fields[blockIndex].Field;
            if (value < 0 || value >= field.Length)
                return;

            if (blockNumber == 13)
            {
                if (value >= 0 && value < uniqueIdentifiers.SkinTone.Length)
                {
                    uniqueIdentifiers.SkinTone[value] =
                        GenerateSkinToneComponent(value, intensity, 1.0f);
                }
                return;
            }

            field[value] = GenerateRandomHexValue(field[value], intensity, 1.0f);
        }

        private void ModifyUniqueIdentifiers(UniqueIdentifiersData uniqueIdentifiers, float intensity, float duration)
        {
            var fields = new List<(string[] Field, string Name)>
            {
                (uniqueIdentifiers.HairColorR, nameof(uniqueIdentifiers.HairColorR)),
                (uniqueIdentifiers.HairColorG, nameof(uniqueIdentifiers.HairColorG)),
                (uniqueIdentifiers.HairColorB, nameof(uniqueIdentifiers.HairColorB)),
                (uniqueIdentifiers.SecondaryHairColorR, nameof(uniqueIdentifiers.SecondaryHairColorR)),
                (uniqueIdentifiers.SecondaryHairColorG, nameof(uniqueIdentifiers.SecondaryHairColorG)),
                (uniqueIdentifiers.SecondaryHairColorB, nameof(uniqueIdentifiers.SecondaryHairColorB)),
                (uniqueIdentifiers.BeardColorR, nameof(uniqueIdentifiers.BeardColorR)),
                (uniqueIdentifiers.BeardColorG, nameof(uniqueIdentifiers.BeardColorG)),
                (uniqueIdentifiers.BeardColorB, nameof(uniqueIdentifiers.BeardColorB)),
                (uniqueIdentifiers.SkinTone, nameof(uniqueIdentifiers.SkinTone)),
                (uniqueIdentifiers.FurColorR, nameof(uniqueIdentifiers.FurColorR)),
                (uniqueIdentifiers.FurColorG, nameof(uniqueIdentifiers.FurColorG)),
                (uniqueIdentifiers.FurColorB, nameof(uniqueIdentifiers.FurColorB)),
                (uniqueIdentifiers.HeadAccessoryColorR, nameof(uniqueIdentifiers.HeadAccessoryColorR)),
                (uniqueIdentifiers.HeadAccessoryColorG, nameof(uniqueIdentifiers.HeadAccessoryColorG)),
                (uniqueIdentifiers.HeadAccessoryColorB, nameof(uniqueIdentifiers.HeadAccessoryColorB)),
                (uniqueIdentifiers.HeadMarkingColorR, nameof(uniqueIdentifiers.HeadMarkingColorR)),
                (uniqueIdentifiers.HeadMarkingColorG, nameof(uniqueIdentifiers.HeadMarkingColorG)),
                (uniqueIdentifiers.HeadMarkingColorB, nameof(uniqueIdentifiers.HeadMarkingColorB)),
                (uniqueIdentifiers.BodyMarkingColorR, nameof(uniqueIdentifiers.BodyMarkingColorR)),
                (uniqueIdentifiers.BodyMarkingColorG, nameof(uniqueIdentifiers.BodyMarkingColorG)),
                (uniqueIdentifiers.BodyMarkingColorB, nameof(uniqueIdentifiers.BodyMarkingColorB)),
                (uniqueIdentifiers.TailMarkingColorR, nameof(uniqueIdentifiers.TailMarkingColorR)),
                (uniqueIdentifiers.TailMarkingColorG, nameof(uniqueIdentifiers.TailMarkingColorG)),
                (uniqueIdentifiers.TailMarkingColorB, nameof(uniqueIdentifiers.TailMarkingColorB)),
                (uniqueIdentifiers.EyeColorR, nameof(uniqueIdentifiers.EyeColorR)),
                (uniqueIdentifiers.EyeColorG, nameof(uniqueIdentifiers.EyeColorG)),
                (uniqueIdentifiers.EyeColorB, nameof(uniqueIdentifiers.EyeColorB)),
                (uniqueIdentifiers.Gender, nameof(uniqueIdentifiers.Gender)),
                (uniqueIdentifiers.BeardStyle, nameof(uniqueIdentifiers.BeardStyle)),
                (uniqueIdentifiers.HairStyle, nameof(uniqueIdentifiers.HairStyle)),
                (uniqueIdentifiers.HeadAccessoryStyle, nameof(uniqueIdentifiers.HeadAccessoryStyle)),
                (uniqueIdentifiers.HeadMarkingStyle, nameof(uniqueIdentifiers.HeadMarkingStyle)),
                (uniqueIdentifiers.BodyMarkingStyle, nameof(uniqueIdentifiers.BodyMarkingStyle)),
                (uniqueIdentifiers.TailMarkingStyle, nameof(uniqueIdentifiers.TailMarkingStyle))
            };

            int fieldsToModify = Math.Clamp((int)intensity, 1, 3);
            for (int i = 0; i < fieldsToModify; i++)
            {
                var fieldIndex = _random.Next(fields.Count);
                var fieldName = fields[fieldIndex].Name;
                var field = fields[fieldIndex].Field;

                if (fieldName == nameof(uniqueIdentifiers.SkinTone))
                {
                    for (int j = 0; j < field.Length; j++)
                    {
                        field[j] = GenerateSkinToneComponent(j, intensity, duration);
                    }
                    continue;
                }

                for (int j = 0; j < field.Length; j++)
                {
                    field[j] = GenerateRandomHexValue(field[j], intensity, duration);
                }
            }
        }

        private void ModifyEnzymesPrototypes(List<EnzymesPrototypeInfo> enzymesPrototypes, string block, int value, float intensity)
        {
            if (_random.NextFloat() < 0.025f)
            {
                var randomEnzyme = enzymesPrototypes[_random.Next(enzymesPrototypes.Count)];
                int randomIndex = _random.Next(0, randomEnzyme.HexCode.Length);

                randomEnzyme.HexCode[randomIndex] = GenerateRandomHexValue(randomEnzyme.HexCode[randomIndex], intensity, 1.0f);
                return;
            }

            if (!int.TryParse(block, out int blockNumber))
                return;

            int blockIndex = blockNumber - 1;
            if (blockIndex < 0 || blockIndex >= enzymesPrototypes.Count)
                return;

            var enzyme = enzymesPrototypes[blockIndex];
            if (value < 0 || value >= enzyme.HexCode.Length)
                return;

            enzyme.HexCode[value] = GenerateRandomHexValue(enzyme.HexCode[value], intensity, 1.0f);
        }

        private void ModifyEnzymesPrototypes(List<EnzymesPrototypeInfo> enzymesPrototypes, float intensity, float duration)
        {
            int itemsToModify = Math.Clamp((int)intensity, 1, 2);
            for (int i = 0; i < itemsToModify; i++)
            {
                var enzymeIndex = _random.Next(enzymesPrototypes.Count);
                var enzyme = enzymesPrototypes[enzymeIndex];
                for (int j = 0; j < enzyme.HexCode.Length; j++)
                {
                    enzyme.HexCode[j] = GenerateRandomHexValue(enzyme.HexCode[j], intensity, duration);
                }
            }
        }

        private string GenerateRandomHexValue(string value, float intensity, float duration)
        {
            int baseValue = Convert.ToInt32(value, 16);

            float changeStrength = Math.Clamp((intensity * duration) / 100f, 0f, 1f);
            changeStrength = (float)Math.Sqrt(changeStrength);

            var maxStep = Math.Clamp((int)Math.Ceiling(15 * changeStrength), 1, 15);
            var offset = _random.Next(1, maxStep + 1);
            if (_random.Prob(0.5f))
                offset *= -1;

            int modifiedValue = (baseValue + offset) % 16;
            if (modifiedValue < 0)
                modifiedValue += 16;

            return modifiedValue.ToString("X1");
        }

        private string GenerateSkinToneComponent(int index, float intensity, float duration)
        {
            switch (index)
            {
                case 0: return (_random.NextFloat() < intensity / 100f) ? "1" : "0";
                case 1: int digit1 = _random.Next(0, 10); return digit1.ToString("X1");
                case 2: int digit2 = _random.Next(0, 10); return digit2.ToString("X1");
                default: return "0";
            }
        }

        private void AddRadiationDamage(EntityUid uid, float intensity)
        {
            float randomMultiplier = 1.5f + _random.NextFloat() * 1.5f;
            var damage = new DamageSpecifier { DamageDict = { { RadDamage, randomMultiplier * intensity } } };
            _damage.TryChangeDamage(uid, damage, ignoreResistances: true, origin: uid);
        }
        #endregion
    }
}
