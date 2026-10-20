#if DEBUG
using System;
using System.Collections;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace MiniTurret;

// Excluded from compilation in every non-Debug configuration as well as by #if.
internal sealed class DebugStageSpawner : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Let the last generation RPC, ItemAttributes.Start and player setup finish.
        yield return null;
        yield return null;
        if (!SemiFunc.IsMasterClientOrSingleplayer() || !SemiFunc.RunIsLevel() ||
            LevelGenerator.Instance == null || !LevelGenerator.Instance.Generated)
        {
            yield break;
        }

        try
        {
            SpawnTestItems();
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError($"Debug stage items could not be spawned: {exception}");
        }
    }

    private static void SpawnTestItems()
    {
        if (StatsManager.instance == null || TruckSafetySpawnPoint.instance == null)
        {
            throw new InvalidOperationException("The stage item catalog or truck spawn point is unavailable.");
        }

        Item? miniCart = null;
        Item? cannon = null;
        Item? laser = null;
        foreach (Item item in StatsManager.instance.itemDictionary.Values)
        {
            if (item == null || item.disabled || item.prefab == null || !item.prefab.IsValid())
            {
                continue;
            }

            GameObject prefab = item.prefab.Prefab;
            if (prefab == null)
            {
                continue;
            }

            PhysGrabCart cart = prefab.GetComponent<PhysGrabCart>();
            if (cart != null && cart.isSmallCart)
            {
                miniCart ??= item;
            }
            if (prefab.GetComponent<ItemCartCannon>() != null)
            {
                cannon ??= item;
            }
            if (prefab.GetComponent<ItemCartLaser>() != null)
            {
                laser ??= item;
            }
            if (miniCart != null && cannon != null && laser != null)
            {
                break;
            }
        }

        // Resolve the entire set first; missing definitions must not produce a partial set.
        if (miniCart == null || cannon == null || laser == null)
        {
            throw new InvalidOperationException("Could not find vanilla mini cart, cart cannon, and cart laser prefabs.");
        }

        Transform anchor = TruckSafetySpawnPoint.instance.transform;
        Item[] items = { miniCart, cannon, laser };
        for (int i = 0; i < items.Length; i++)
        {
            // Separate loose items, ready for manual installation/testing. Use
            // prefab volume offsets like normal item spawning, not a custom mount.
            Vector3 position = anchor.TransformPoint(new Vector3((i - 1) * 1.4f, 0.5f, 1.5f));
            Quaternion rotation = anchor.rotation * items[i].spawnRotationOffset;
            if (SemiFunc.IsMultiplayer())
            {
                PhotonNetwork.InstantiateRoomObject(items[i].prefab.ResourcePath, position, rotation, 0);
            }
            else
            {
                Instantiate(items[i].prefab.Prefab, position, rotation);
            }
        }

        Plugin.ModLogger.LogInfo("Debug stage items spawned: one mini cart, one cart cannon, one cart laser.");
    }
}

[HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
internal static class DebugStageStartPatch
{
    [HarmonyPostfix]
    private static void Postfix(LevelGenerator __instance)
    {
        if (__instance.Generated && SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.RunIsLevel() &&
            __instance.GetComponent<DebugStageSpawner>() == null)
        {
            // The component is the per-generator once guard; a repeated GenerateDone
            // or a newly joined player cannot spawn another set in the same stage.
            __instance.gameObject.AddComponent<DebugStageSpawner>();
        }
    }
}
#endif
