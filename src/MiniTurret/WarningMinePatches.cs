using HarmonyLib;

namespace MiniTurret;

// Only warning props carry MineWarningCarrier. Ordinary purchased mines retain
// all their native behavior. The host never runs warning mine state transitions.
[HarmonyPatch(typeof(ItemMine), "Start")]
internal static class WarningMineStartPatch
{
    [HarmonyPostfix]
    private static void Postfix(ItemMine __instance)
    {
        if (__instance.GetComponent<MineWarningCarrier>() != null)
        {
            __instance.enabled = false;
        }
    }
}

[HarmonyPatch(typeof(ItemMine), nameof(ItemMine.StateSetRPC))]
internal static class WarningMineStatePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemMine __instance, int newState) =>
        !MineWarningCarrier.IsWarning(__instance) || newState == (int)ItemMine.States.Arming;
}

// Block transitions before StateSet can broadcast TriggeredRPC to stock peers.
// The warning uses StateSetRPC(Arming) directly; it never needs StateSet.
[HarmonyPatch(typeof(ItemMine), "StateSet")]
internal static class WarningMineTransitionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemMine __instance) =>
        !MineWarningCarrier.IsWarning(__instance);
}

[HarmonyPatch(typeof(ItemMine), nameof(ItemMine.DestroyMine))]
internal static class WarningMineDestroyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemMine __instance) =>
        !MineWarningCarrier.IsWarning(__instance);
}

[HarmonyPatch(typeof(ItemMine), nameof(ItemMine.TriggeredRPC))]
internal static class WarningMineTriggerPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemMine __instance) =>
        !MineWarningCarrier.IsWarning(__instance);
}

[HarmonyPatch(typeof(ItemMineExplosive), nameof(ItemMineExplosive.OnTriggered))]
internal static class WarningMineExplosionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemMineExplosive __instance) =>
        !MineWarningCarrier.IsWarning(__instance);
}

[HarmonyPatch(typeof(ItemMine), "StateArming")]
internal static class VanillaMineArming
{
    [HarmonyReversePatch]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void Tick(ItemMine instance)
    {
        throw new System.NotSupportedException("Harmony reverse patch was not installed.");
    }
}
