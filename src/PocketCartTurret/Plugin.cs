using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace PocketCartTurret;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "artrointel.PocketCartTurret";
    public const string PluginName = "PocketCartTurret";
    public const string PluginVersion = "0.1.2";

    internal static ManualLogSource ModLogger { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        ModLogger = Logger;
        _harmony = new Harmony(PluginGuid);
        try
        {
            // Resolve all private field bindings before installing any hooks.
            _ = VanillaWeapon.State;
            _harmony.PatchAll();
        }
        catch (Exception exception)
        {
            _harmony.UnpatchSelf();
            ModLogger.LogError($"{PluginName} initialization failed; patches removed: {exception}");
            enabled = false;
            return;
        }

        ModLogger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
#if DEBUG
        ModLogger.LogWarning("Debug build: one mini cart, cannon, and laser will spawn at each mission start.");
#endif
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}

