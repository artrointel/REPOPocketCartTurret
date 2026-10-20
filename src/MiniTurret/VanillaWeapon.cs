using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace MiniTurret;

// Access the live game's non-public fields without changing game assemblies.
internal static class VanillaWeapon
{
    internal static readonly AccessTools.FieldRef<ItemCartCannonMain, ItemCartCannonMain.state> State =
        AccessTools.FieldRefAccess<ItemCartCannonMain, ItemCartCannonMain.state>("stateCurrent");
    internal static readonly AccessTools.FieldRef<PhysGrabObjectImpactDetector, PhysGrabCart> Cart =
        AccessTools.FieldRefAccess<PhysGrabObjectImpactDetector, PhysGrabCart>("currentCart");
    internal static readonly AccessTools.FieldRef<ItemBattery, int> BatteryBars =
        AccessTools.FieldRefAccess<ItemBattery, int>("batteryLifeInt");
    internal static readonly AccessTools.FieldRef<ItemBattery, float> BatteryLife =
        AccessTools.FieldRefAccess<ItemBattery, float>("batteryLife");
    internal static readonly AccessTools.FieldRef<EnemyParent, Enemy> Enemy =
        AccessTools.FieldRefAccess<EnemyParent, Enemy>("Enemy");
    internal static readonly AccessTools.FieldRef<EnemyParent, bool> Spawned =
        AccessTools.FieldRefAccess<EnemyParent, bool>("Spawned");
    internal static readonly AccessTools.FieldRef<EnemyHealth, bool> Dead =
        AccessTools.FieldRefAccess<EnemyHealth, bool>("dead");
}

[HarmonyPatch(typeof(ItemCartCannonMain), "StateSet")]
internal static class VanillaWeaponState
{
    // Execute the original method, including its master-only vanilla StateSetRPC.
    [HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Set(ItemCartCannonMain instance, int state)
    {
        throw new NotSupportedException("Harmony reverse patch was not installed.");
    }
}
