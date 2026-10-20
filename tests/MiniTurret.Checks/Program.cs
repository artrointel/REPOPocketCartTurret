using System.Numerics;
using MiniTurret;
using Mono.Cecil;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}

bool ready = TurretRules.CanAutomate(true, true, true, true, true, false);
Check(ready, "A powered mounted mini-cart weapon must activate.");
for (int disabled = 0; disabled < 6; disabled++)
{
    bool[] flags = { true, true, true, true, true, false };
    flags[disabled] = !flags[disabled];
    Check(!TurretRules.CanAutomate(flags[0], flags[1], flags[2], flags[3], flags[4], flags[5]),
        $"Automation gate {disabled} did not disable the turret.");
}

Check(TurretRules.InRange(17.5f * 17.5f, TurretRules.CannonRange), "Cannon boundary rejected.");
Check(!TurretRules.InRange(17.51f * 17.51f, TurretRules.CannonRange), "Cannon exceeds 70% range.");
Check(TurretRules.InRange(10.5f * 10.5f, TurretRules.LaserRange), "Laser boundary rejected.");
Check(!TurretRules.InRange(10.51f * 10.51f, TurretRules.LaserRange), "Laser exceeds 70% range.");
Check(!TurretRules.InRange(float.NaN, TurretRules.CannonRange), "NaN distance accepted.");
Check(TurretRules.PreferCandidate(4f, 20, 9f, 1), "Nearest enemy not selected.");
Check(!TurretRules.PreferCandidate(16f, 1, 9f, 20), "Farther enemy replaced a nearer enemy.");
Check(TurretRules.PreferCandidate(9f, 1, 9f, 20), "Equal-distance tie is unstable.");
Check(TurretRules.StopState(2) == 1, "Lost buildup target should cancel without battery cost.");
Check(TurretRules.StopState(3) == 4, "Stopping laser fire must go through battery-consuming cooldown.");
Check(TurretRules.StopState(4) == null, "Cooldown must not restart and drain the battery twice.");
Check(TurretRules.StopState(0) == null && TurretRules.StopState(1) == null, "Idle state was interrupted.");

var warning = new DetectionWarning();
Check(!warning.Observe(true, false, 0f) && !warning.CanFire(0f), "No enemy produced a warning or shot.");
Check(!warning.Observe(false, true, 1f) && !warning.CanFire(1f), "Ineligible weapon started a warning.");
Check(warning.Observe(true, true, 2f) && warning.FirstShotPending, "First acquisition must spawn one warning item.");
Check(!warning.CanFire(2f) && !warning.CanFire(3f) && !warning.CanFire(3.999f),
    "Automatic fire escaped the full two-second tracking hold.");
Check(warning.CanFire(4f), "First automatic shot was not released at two seconds.");
Check(!warning.CanFire(4f, 2f, false), "Elapsed hold must not fire through an unaligned or obstructed barrel.");
Check(!warning.CanFire(4f, 4.5f, true), "Elapsed hold must not skip a subsequent native preparation time.");
Check(warning.CanFire(4.5f, 4.5f, true), "Aligned weapon did not release after both clocks elapsed.");
Check(!warning.Observe(true, true, 4.5f), "Continuous tracking/nearest-target change repeated the warning.");
warning.MarkFired();
Check(!warning.FirstShotPending && warning.CanFire(4.6f), "Subsequent native firing cycle restarted the warning.");
Check(!warning.Observe(true, false, 5f) && !warning.CanFire(5f), "Target loss did not immediately block fire.");
Check(warning.Observe(true, true, 5.2f) && warning.FirstShotPending &&
    !warning.CanFire(7.199f) && warning.CanFire(7.2f),
    "Reacquisition after a shot needs a fresh warning item and two continuous seconds.");
warning.Observe(false, true, 7.3f);
Check(!warning.CanFire(100f), "Unmounting, no power, or grabbing the weapon left firing armed.");
Check(warning.Observe(true, true, 8f) && !warning.CanFire(9.999f) && warning.CanFire(10f),
    "Eligibility recovery must start a fresh two-second hold.");
warning.Reset();
Check(!warning.CanFire(100f) && !warning.FirstShotPending, "Disabling the weapon retained the warning session.");

var flickerWarning = new DetectionWarning();
Check(flickerWarning.Observe(true, true, 10f) && flickerWarning.KeepItem, "First warning must retain its item.");
Check(!flickerWarning.Observe(true, false, 11.9f) && flickerWarning.KeepItem && !flickerWarning.CanFire(11.9f),
    "Brief tracking loss must keep the pending item but block shooting.");
Check(!flickerWarning.Observe(true, true, 12f) && flickerWarning.KeepItem &&
    !flickerWarning.CanFire(12f) && !flickerWarning.CanFire(13.999f) && flickerWarning.CanFire(14f),
    "Even brief loss must restart two continuous seconds without respawning the pending item.");
flickerWarning.Observe(true, false, 14.1f);
flickerWarning.Observe(true, false, 14.7f);
Check(!flickerWarning.KeepItem, "Past-grace loss must discard the warning item.");
Check(flickerWarning.Observe(true, true, 15f) &&
    !flickerWarning.CanFire(16.999f) && flickerWarning.CanFire(17f),
    "Past-grace reacquisition must restart the full two-second hold at zero.");
flickerWarning.MarkFired();
Check(!flickerWarning.KeepItem, "A completed first shot must not retain a warning item.");
// Sweep a full circle with several offset barrel pivots and fixed pitches.
// The solved yaw must intersect the target's horizontal coordinates while
// preserving the original vertical component of the firing direction.
foreach (float pitch in new[] { -0.3f, 0f, 0.3f })
foreach (float lateralOffset in new[] { -0.8f, 0f, 0.8f })
for (int bearing = -180; bearing < 180; bearing += 5)
{
    Vector3 forward = Vector3.Normalize(new Vector3(0, pitch, 1));
    Vector3 muzzle = new(lateralOffset, 1.2f, 0.7f);
    float radians = bearing * MathF.PI / 180f;
    Vector3 target = new(MathF.Sin(radians) * 8f, 1.2f, MathF.Cos(radians) * 8f);
    Check(TurretRules.TryAimYaw(forward.X, forward.Z, muzzle.X, muzzle.Z, target.X, target.Z, out float yaw),
        $"Reachable bearing {bearing} was rejected.");
    Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw * MathF.PI / 180f);
    Vector3 direction = Vector3.Transform(forward, rotation);
    Vector3 origin = Vector3.Transform(muzzle, rotation);
    float distance = ((target.X - origin.X) * direction.X + (target.Z - origin.Z) * direction.Z) /
        (direction.X * direction.X + direction.Z * direction.Z);
    Vector3 hit = origin + distance * direction;
    Check(distance > 0f && MathF.Abs(hit.X - target.X) < 0.0001f && MathF.Abs(hit.Z - target.Z) < 0.0001f,
        $"Offset barrel yaw misses at bearing {bearing}.");
    Check(MathF.Abs(direction.Y - forward.Y) < 0.0001f && MathF.Abs(origin.Y - muzzle.Y) < 0.0001f,
        "Yaw aiming changed pitch or muzzle elevation.");
}
Check(!TurretRules.TryAimYaw(0, 0, 0, 0, 0, 5, out _), "Vertical barrel accepted for yaw control.");
Check(!TurretRules.TryAimYaw(0, 1, 2, 0, 0, 1, out _), "Unreachable inner pivot radius accepted.");
Check(!TurretRules.TryAimYaw(0, 1, 0, 2, 0, 1, out _), "Point behind barrel origin accepted.");

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
using AssemblyDefinition debug = AssemblyDefinition.ReadAssembly(Path.Combine(root, "src/MiniTurret/bin/Debug/netstandard2.1/MiniTurret.dll"));
using AssemblyDefinition release = AssemblyDefinition.ReadAssembly(Path.Combine(root, "src/MiniTurret/bin/Release/netstandard2.1/MiniTurret.dll"));
Check(debug.MainModule.Types.Any(t => t.Name == "DebugStageSpawner"), "Debug spawn code missing in Debug.");
Check(debug.MainModule.Types.Any(t => t.Name == "DebugStageStartPatch"), "Debug stage patch missing in Debug.");
Check(!release.MainModule.Types.Any(t => t.Name.StartsWith("DebugStage")), "Release contains debug spawn code.");
Check(!release.MainModule.GetTypeReferences().Any(t => t.Name == "DebugStageSpawner"), "Release still references debug spawn code.");
Check(!release.MainModule.Types.Any(t => t.Name == "WarningPrefabProbe"), "Release contains the Debug startup probe.");
foreach (AssemblyDefinition mod in new[] { debug, release })
{
    foreach (string removed in new[] { "WarningPrefabProbe", "WarningSoundPlaybackPatch", "WarningPrefabWarmupPatch" })
        Check(!mod.MainModule.Types.Any(t => t.Name == removed), $"Diagnostic code still present: {removed}");
    Check(!mod.MainModule.Types.SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i =>
        i.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr && i.Operand is string message &&
        (message.StartsWith("Warning audio result:") || message.StartsWith("Warning visual:") ||
            message.StartsWith("First shot released:") || message == "Turret ")),
        "Per-frame/per-shot diagnostic messages remain in the plugin.");
    Check(!mod.MainModule.Types.SelectMany(t => t.Methods).Any(m =>
        m.CustomAttributes.Any(a => a.AttributeType.Name == "PunRPC")), "A custom RPC would require modded clients.");
    TypeDefinition reversePatch = mod.MainModule.Types.Single(t => t.Name == "VanillaWeaponState");
    Check(reversePatch.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyPatch"),
        "The reverse patch class must be discoverable by Harmony.PatchAll.");
    Check((reversePatch.Methods.Single(m => m.Name == "Set").ImplAttributes & MethodImplAttributes.NoInlining) != 0,
        "The reverse-patch stub must not be inlined by the game runtime.");
    Check(mod.MainModule.Types.Single(t => t.Name == "WeaponStartPatch").Methods.Single(m => m.Name == "Postfix").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "IsMasterClientOrSingleplayer"),
        "Only the fixed host should create turret controllers.");
    TypeDefinition grabPatch = mod.MainModule.Types.Single(t => t.Name == "WeaponGrabPatch");
    Check(grabPatch.Methods.Single(m => m.Name == "Prefix").ReturnType.FullName == "System.Void",
        "Turret must not skip vanilla GrabLogic: it supplies upright stabilization and damping.");
    Check(grabPatch.Methods.Single(m => m.Name == "Prefix").Body.ExceptionHandlers.Any(h =>
        h.HandlerType == Mono.Cecil.Cil.ExceptionHandlerType.Catch),
        "An aiming exception must not escape the prefix and block vanilla stabilization.");
    Check(!mod.MainModule.Types.Any(t => t.Name == "WeaponPhysicsPatch"),
        "Independent torque must not replace or fight the native corrector.");
    TypeDefinition controller = mod.MainModule.Types.Single(t => t.Name == "TurretController");
    Check(!controller.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i =>
        i.Operand is MethodReference method && method.Name == "AddTorque"),
        "Turret is applying torque outside the vanilla stabilizer.");
    Check(!mod.MainModule.Types.Any(t => t.Name == "TurretPose") &&
        !controller.Methods.Any(m => m.Name == "GetMuzzleAtRotation"),
        "Target acquisition must not synthesize a corrected upright muzzle.");
    foreach (string getter in new[] { "get_position", "get_forward" })
        Check(controller.Methods.Single(m => m.Name == "SweptRayHits").Body.Instructions.Any(i =>
            i.Operand is MethodReference method && method.Name == getter),
            $"Target acquisition must read the actual muzzle's {getter}.");
    TypeDefinition shotGate = mod.MainModule.Types.Single(t => t.Name == "WeaponShotGatePatch");
    Check(shotGate.Methods.Single(m => m.Name == "Prefix").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "PrepareAutomaticShot"),
        "Vanilla FixedUpdate/StateSet must also obey the warning gate.");
    Check(controller.Methods.Single(m => m.Name == "PrepareAutomaticShot").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "RemoveWarningItem"),
        "Firing must remove the temporary warning item before the shooting RPC.");
    Check(controller.Methods.Single(m => m.Name == "StopAutomaticCycle").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "RemoveWarningItem"),
        "Cancel/disable cleanup must remove the temporary warning item.");
    Check(controller.Methods.Single(m => m.Name == "BeforeWeaponUpdate").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "get_KeepItem"),
        "Warning item cleanup must obey the same grace period as the warning timer.");
    Check(controller.Methods.Single(m => m.Name == "BeforeWeaponUpdate").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "RenewLease"),
        "A pending warning must renew its lifetime through long tracking/alignment waits.");
    Check(mod.MainModule.Types.Any(t => t.Name == "MineWarningCarrier"), "Warning Explosive Mine missing from the build.");
    foreach (string patch in new[] { "WarningItemStatsPatch", "WarningItemListPatch", "WarningItemGrabPatch", "WarningItemEquipPatch" })
        Check(mod.MainModule.Types.Any(t => t.Name == patch), $"Temporary warning item isolation missing: {patch}");
    TypeDefinition carrier = mod.MainModule.Types.Single(t => t.Name == "MineWarningCarrier");
    Check(!mod.MainModule.GetTypeReferences().Any(t => t.Name == "ItemWalkieTalkie" || t.Name == "ItemReviveItem"),
        "Warning props must not retain walkie-talkie or DEFIBRO dependencies.");
    Check(carrier.Methods.Single(m => m.Name == "Create").Parameters.Single().ParameterType.FullName == "UnityEngine.Transform",
        "The mine must be anchored to a weapon transform.");
    Check(!carrier.Fields.Any(f => f.Name == "_cartOffset" || f.Name == "_cart"),
        "A cargo/cart offset must not displace the warning mine from the weapon.");
    Check(carrier.Methods.Single(m => m.Name == "PinToMount").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.Name == "get_position"),
        "The warning mine must follow the current weapon position.");
    Check(carrier.Methods.Single(m => m.Name == "Create").Body.Instructions.Count(i =>
        i.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr && Equals(i.Operand, "StateSetRPC")) == 1,
        "Warning mine must send one stock state RPC for vanilla peers.");
    Check(carrier.Methods.Single(m => m.Name == "StartArming").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.DeclaringType.Name == "VanillaMineArming" && method.Name == "Tick"),
        "Host warning must run the native Arming sound logic.");
    Check(carrier.Methods.Single(m => m.Name == "Update").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.DeclaringType.Name == "VanillaMineArming" && method.Name == "Tick"),
        "The host must continue the native arming cadence until warning cleanup.");
    Check((mod.MainModule.Types.Single(t => t.Name == "VanillaMineArming").Methods.Single(m =>
        m.Name == "Tick").ImplAttributes & MethodImplAttributes.NoInlining) != 0,
        "The native arming reverse patch must not be inlined.");
    Check(!carrier.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i =>
        i.Operand is MethodReference method && (method.Name == "SetTriggered" || method.Name == "DestroyMine" ||
            method.Name == "OnTriggered" || method.Name == "Spawn" || method.Name == "GrabStarted")),
        "Warning mine must not invoke explosions, grab a player, or use destructive mine cleanup.");
    foreach (string safetyPatch in new[] { "WarningMineStartPatch", "WarningMineStatePatch", "WarningMineTransitionPatch", "WarningMineDestroyPatch", "WarningMineTriggerPatch", "WarningMineExplosionPatch" })
        Check(mod.MainModule.Types.Any(t => t.Name == safetyPatch), $"Missing warning mine safeguard: {safetyPatch}");
    Check(carrier.Methods.Single(m => m.Name == "Dispose").Body.Instructions.Any(i =>
        i.Operand is MethodReference method && method.DeclaringType.Name == "PhotonNetwork" && method.Name == "Destroy"),
        "Temporary warning item destruction must propagate to vanilla clients.");
    Check(!carrier.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i =>
        i.Operand is MethodReference method && method.DeclaringType.Name == "StatsManager" && method.Name == "ItemRemove"),
        "Temporary warning item cleanup must not decrement the player's purchased warning item count.");
}

// Verify the patch/field contract against the actual installed game, not just
// the publicized compile references. No game assembly is executed or distributed.
string gamePath = args.Length > 0 ? args[0] : @"C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(gamePath);
TypeDefinition GameType(string name) => game.MainModule.Types.Single(t => t.Name == name);
foreach (string method in new[] { "Start", "Update", "FixedUpdate", "GrabLogic", "StateSet", "StateSetRPC" })
    Check(GameType("ItemCartCannonMain").Methods.Any(m => m.Name == method), $"Game weapon method missing: {method}");
Check(GameType("LevelGenerator").Methods.Any(m => m.Name == "GenerateDone"), "Stage completion hook missing.");
TypeDefinition mine = GameType("ItemMine");
Check(mine.Fields.Any(f => f.Name == "soundArmingBeep" && f.FieldType.Name == "Sound"), "Mine Arming sound missing.");
MethodDefinition mineStateRpc = mine.Methods.Single(m => m.Name == "StateSetRPC");
Check(mineStateRpc.CustomAttributes.Any(a => a.AttributeType.Name == "PunRPC") &&
    mineStateRpc.Parameters.Count == 2 && mineStateRpc.Parameters[0].ParameterType.FullName == "System.Int32",
    "Native mine state RPC contract changed.");
Check(!mineStateRpc.Body.Instructions.Any(i => i.Operand is MethodReference m &&
    (m.Name == "Invoke" || m.Name == "OnTriggered" || m.Name == "Spawn")),
    "Setting a mine's state must not invoke an explosion callback.");
MethodDefinition arming = mine.Methods.Single(m => m.Name == "StateArming");
Check(arming.Body.Instructions.Count(i => i.Operand is FieldReference f && f.Name == "soundArmingBeep") == 2,
    "Native Arming state must play an initial beep and repeat it.");
Check(arming.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "get_position"),
    "The arming beep must originate at the mine.");
Check(mine.NestedTypes.Single(t => t.Name == "States").Fields.Any(f => f.Name == "Arming" && Equals(f.Constant, 1)),
    "Native Arming state value changed.");
Check(GameType("ItemMineExplosive").Methods.Any(m => m.Name == "OnTriggered"), "Explosive mine guard hook missing.");
foreach ((string type, string method) in new[] { ("StatsManager", "ItemFetchName"), ("ItemManager", "AddSpawnedItem"),
    ("PhysGrabObject", "GrabStartedRPC"), ("ItemEquippable", "RPC_RequestEquip"), ("ItemToggle", "ToggleDisableRPC") })
    Check(GameType(type).Methods.Any(m => m.Name == method), $"Warning Explosive Mine isolation hook missing: {type}.{method}");
foreach ((string type, string field, string expectedType) in new[]
{
    ("ItemCartCannonMain", "stateCurrent", "ItemCartCannonMain/state"),
    ("ItemCartCannonMain", "currentCorrector", "UnityEngine.GameObject"),
    ("PhysGrabObjectImpactDetector", "currentCart", "PhysGrabCart"),
    ("ItemBattery", "batteryLifeInt", "System.Int32"),
    ("ItemBattery", "batteryLife", "System.Single"),
    ("EnemyParent", "Enemy", "Enemy"),
    ("EnemyParent", "Spawned", "System.Boolean"),
    ("EnemyHealth", "dead", "System.Boolean")
})
    Check(GameType(type).Fields.Any(f => f.Name == field && f.FieldType.FullName == expectedType),
        $"Live game field contract changed: {type}.{field}");
foreach ((string name, int value) in new[] { ("inactive", 0), ("active", 1), ("buildup", 2), ("shooting", 3), ("goingBack", 4) })
    Check(GameType("ItemCartCannonMain").NestedTypes.Single(t => t.Name == "state").Fields.Any(f =>
        f.Name == name && Equals(f.Constant, value)), $"Weapon state value changed: {name}");
Check(GameType("ItemCartCannon").Methods.Single(m => m.Name == "ShootLogic").Body.Instructions.Any(i =>
    i.Operand is float value && value == TurretRules.CannonRange), "Vanilla cannon range changed.");
Check(GameType("ItemCartLaser").Methods.Single(m => m.Name == "StateShooting").Body.Instructions.Any(i =>
    i.Operand is float value && value == TurretRules.LaserRange), "Vanilla laser range changed.");
Console.WriteLine($"Passed {checks} checks: authority, energy/mount gates, range, nearest target, 360-degree yaw geometry, actual muzzle acquisition, native stabilization preservation, two-second warning, mine warning/cleanup/isolation, live game contract, Release exclusion.");
