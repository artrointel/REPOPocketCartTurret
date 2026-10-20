using System;
using HarmonyLib;

namespace PocketCartTurret;

[HarmonyPatch(typeof(ItemCartCannonMain), "Start")]
internal static class WeaponStartPatch
{
    [HarmonyPostfix]
    private static void Postfix(ItemCartCannonMain __instance)
    {
        if (SemiFunc.IsMasterClientOrSingleplayer() &&
            (__instance.GetComponent<ItemCartCannon>() != null || __instance.GetComponent<ItemCartLaser>() != null) &&
            __instance.GetComponent<TurretController>() == null)
        {
            __instance.gameObject.AddComponent<TurretController>().Initialize(__instance);
        }
    }
}

[HarmonyPatch(typeof(ItemCartCannonMain), "Update")]
internal static class WeaponUpdatePatch
{
    [HarmonyPrefix]
    private static void Prefix(ItemCartCannonMain __instance) =>
        __instance.GetComponent<TurretController>()?.BeforeWeaponUpdate();
}

[HarmonyPatch(typeof(ItemCartCannonMain), "GrabLogic")]
internal static class WeaponGrabPatch
{
    [HarmonyPrefix]
    private static void Prefix(ItemCartCannonMain __instance)
    {
        // Keep the entire vanilla method: it provides upright stabilization,
        // angular damping and manual grab behavior. Only steer its yaw target.
        TurretController controller = __instance.GetComponent<TurretController>();
        try
        {
            controller?.BeforeVanillaGrab();
        }
        catch (Exception exception)
        {
            // An automation failure must never prevent the game's stabilizer.
            controller?.DisableAutomation(exception);
        }
    }
}

[HarmonyPatch(typeof(ItemCartCannonMain), "StateSet")]
internal static class WeaponShotGatePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemCartCannonMain __instance, int state)
    {
        // StateSet can also be reached by vanilla FixedUpdate. Guard that path
        // too, rather than only delaying the controller's Update code.
        if (state != (int)ItemCartCannonMain.state.shooting || !SemiFunc.IsMasterClientOrSingleplayer())
        {
            return true;
        }
        TurretController controller = __instance.GetComponent<TurretController>();
        return controller == null || controller.PrepareAutomaticShot();
    }
}
