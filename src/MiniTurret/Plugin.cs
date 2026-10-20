using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MiniTurret;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "artrointel.MiniTurret";
    public const string PluginName = "MiniTurret";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource ModLogger { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        ModLogger = Logger;
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

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

