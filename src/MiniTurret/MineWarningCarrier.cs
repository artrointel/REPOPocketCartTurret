using System;
using System.Collections;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace MiniTurret;

// Host-only carrier. Stock peers receive the mine, its Arming state RPC, and destruction.
internal sealed class MineWarningCarrier : MonoBehaviour
{
    private static Item? _mineItem;
    private Transform _mount = null!;
    private PhysGrabObject _physical = null!;
    private Rigidbody _body = null!;
    private float _leaseExpiresAt;
    private bool _disposing;
    private Collider[] _colliders = Array.Empty<Collider>();
    private ItemMine _mine = null!;
    private bool _armingStarted;

    internal static bool IsWarning(Component component) =>
        component.GetComponent<MineWarningCarrier>() != null;

    internal void RenewLease() => _leaseExpiresAt = Time.time + 5f;

    internal static MineWarningCarrier Create(Transform mount)
    {
        Item item = FindMine();
        Vector3 position = mount.position;
        GameObject instance = SemiFunc.IsMultiplayer()
            ? PhotonNetwork.InstantiateRoomObject(item.prefab.ResourcePath, position, mount.rotation, 0)
            : Instantiate(item.prefab.Prefab, position, mount.rotation);
        MineWarningCarrier? carrier = null;
        try
        {
            carrier = instance.AddComponent<MineWarningCarrier>();
            carrier._mount = mount;
            carrier._physical = instance.GetComponent<PhysGrabObject>();
            carrier._body = instance.GetComponent<Rigidbody>();
            carrier._mine = instance.GetComponent<ItemMine>();
            carrier.RenewLease();
            carrier._colliders = instance.GetComponentsInChildren<Collider>(true);
            carrier.PinToMount();
            carrier._mine.triggeredByEnemies = false;
            carrier._mine.triggeredByPlayers = false;
            carrier._mine.triggeredByRigidBodies = false;
            carrier._mine.triggeredByForces = false;
            instance.GetComponent<PhysGrabObjectImpactDetector>().destroyDisable = true;
            foreach (ItemMineTrigger trigger in instance.GetComponentsInChildren<ItemMineTrigger>(true))
            {
                trigger.enabled = false;
            }
            // This RPC only sets state/timers; it is safe before native Start.
            // Send before the first Update so peers don't play a spawn/disarm beep.
            PhotonView view = instance.GetComponent<PhotonView>();
            if (SemiFunc.IsMultiplayer())
            {
                // Cache just these initial settings for a late-joining peer.
                // PhotonNetwork.Destroy removes this object's buffered RPCs.
                view.RPC("ToggleDisableRPC", RpcTarget.AllBuffered, true);
                view.RPC("StateSetRPC", RpcTarget.AllBuffered, (int)ItemMine.States.Arming);
            }
            else
            {
                carrier._mine.StateSetRPC((int)ItemMine.States.Arming);
            }

            instance.SetActive(true);
            return carrier;
        }
        catch
        {
            if (carrier != null)
            {
                carrier.Dispose();
            }
            else if (SemiFunc.IsMultiplayer())
            {
                PhotonNetwork.Destroy(instance);
            }
            else
            {
                Destroy(instance);
            }
            throw;
        }
    }

    private IEnumerator Start()
    {
        // Wait until all native Start methods and their first-frame icon/setup
        // work have completed. Position the actual transform before sound playback.
        yield return null;
        if (_disposing || _mount == null)
        {
            yield break;
        }
        PinToMount();

        // Native startup may teleport the ItemVolume origin. Synchronize the
        // corrected position before arming sound playback and stock position synchronization.
        _physical.Teleport(transform.position, transform.rotation);
        StartArming();
    }

    private void StartArming()
    {
        if (!_armingStarted && SemiFunc.IsMasterClientOrSingleplayer())
        {
            _armingStarted = true;
            VanillaMineArming.Tick(_mine);
        }
    }

    private void Update()
    {
        if (_armingStarted && !_disposing && SemiFunc.IsMasterClientOrSingleplayer())
        {
            // Run the exact stock arming cadence. Native StateSet is blocked on
            // this prop, so its timer can never arm or detonate the warning mine.
            VanillaMineArming.Tick(_mine);
        }
    }

    private static Item FindMine()
    {
        if (_mineItem != null)
        {
            return _mineItem;
        }
        foreach (Item item in StatsManager.instance.itemDictionary.Values)
        {
            if (item != null && item.prefab != null && item.prefab.IsValid() &&
                item.prefab.Prefab != null && item.prefab.Prefab.GetComponent<ItemMineExplosive>() != null)
            {
                Sound sound = item.prefab.Prefab.GetComponent<ItemMine>().soundArmingBeep;
                if (sound == null || sound.Sounds == null || sound.Sounds.Length == 0)
                {
                    throw new InvalidOperationException("The vanilla Explosive Mine has no arming sound.");
                }
                _mineItem = item;
                return item;
            }
        }
        throw new InvalidOperationException("The vanilla Explosive Mine prefab (ItemMine) was not found.");
    }

    private void LateUpdate()
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer())
        {
            return;
        }
        // The controller renews this lease while tracking. An abandoned prop expires.
        if (_mount == null || Time.time >= _leaseExpiresAt)
        {
            Dispose();
            return;
        }
        // Let native Start methods calculate bounds before disabling colliders.
        // Rigidbody collisions were already disabled immediately at creation.
        foreach (Collider collider in _colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        PinToMount();
    }

    private void PinToMount()
    {
        _physical.OverrideKinematic(0.2f);
        _physical.OverrideGrabDisable(0.2f);
        _physical.OverrideIndestructible(0.2f);
        _body.detectCollisions = false;
        _body.interpolation = RigidbodyInterpolation.None;
        Vector3 position = _mount.position;
        transform.SetPositionAndRotation(position, _mount.rotation);
        _body.position = position;
        _body.rotation = _mount.rotation;
    }

    internal void Dispose()
    {
        if (_disposing)
        {
            return;
        }
        bool multiplayer = GameManager.instance != null && SemiFunc.IsMultiplayer();
        if (multiplayer && PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            return;
        }
        _disposing = true;
        if (multiplayer && PhotonNetwork.InRoom)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.ItemFetchName))]
internal static class WarningItemStatsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemAttributes itemAttributes) =>
        itemAttributes == null || !MineWarningCarrier.IsWarning(itemAttributes);
}

[HarmonyPatch(typeof(ItemManager), nameof(ItemManager.AddSpawnedItem))]
internal static class WarningItemListPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemAttributes item) =>
        item == null || !MineWarningCarrier.IsWarning(item);
}

[HarmonyPatch(typeof(PhysGrabObject), "GrabStartedRPC")]
internal static class WarningItemGrabPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PhysGrabObject __instance) =>
        !MineWarningCarrier.IsWarning(__instance);
}

// Equipping has its own RPC and must not rely on the grab approval gate.
[HarmonyPatch(typeof(ItemEquippable), "RPC_RequestEquip")]
internal static class WarningItemEquipPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemEquippable __instance) =>
        !MineWarningCarrier.IsWarning(__instance);
}
