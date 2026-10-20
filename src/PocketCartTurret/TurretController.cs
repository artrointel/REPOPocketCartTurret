using System;
using UnityEngine;

namespace PocketCartTurret;

// Host-local only. The existing weapon RPCs and PhysGrabObject transform sync
// carry firing, damage, effects and physical rotation to unmodified clients.
internal sealed class TurretController : MonoBehaviour
{
    private ItemCartCannonMain _weapon = null!;
    private ItemBattery _battery = null!;
    private PhysGrabObject _physical = null!;
    private PhysGrabObjectImpactDetector _impact = null!;
    private Transform _muzzle = null!;
    private float _range;
    private EnemyParent? _target;
    private bool _automaticCycle;
    private bool _failed;
    private readonly DetectionWarning _warningState = new();
    private MineWarningCarrier? _warningItem;
    private float _cycleReadyAt;

    internal void Initialize(ItemCartCannonMain weapon)
    {
        _weapon = weapon;
        _battery = GetComponent<ItemBattery>();
        _physical = GetComponent<PhysGrabObject>();
        _impact = GetComponent<PhysGrabObjectImpactDetector>();
        Rigidbody body = GetComponent<Rigidbody>();
        ItemCartLaser laser = GetComponent<ItemCartLaser>();
        _muzzle = laser != null ? laser.muzzleTransform : weapon.muzzle;
        _range = laser != null ? TurretRules.LaserRange : TurretRules.CannonRange;
        _failed = _battery == null || _physical == null || _impact == null || body == null || _muzzle == null;
        if (_failed)
        {
            Plugin.ModLogger.LogError($"Cannot initialize turret on {name}: a required vanilla component is missing.");
        }
    }

    internal bool OwnsAim
    {
        get
        {
            if (_failed || GameManager.instance == null || RunManager.instance == null)
            {
                return false;
            }

            PhysGrabCart cart = VanillaWeapon.Cart(_impact);
            return TurretRules.CanAutomate(
                SemiFunc.IsMasterClientOrSingleplayer(),
                LevelGenerator.Instance != null && LevelGenerator.Instance.Generated && SemiFunc.RunIsLevel(),
                cart != null && cart.isSmallCart,
                _impact.inCart && cart != null,
                VanillaWeapon.BatteryBars(_battery) > 0 && VanillaWeapon.BatteryLife(_battery) > 0f,
                _physical.playerGrabbing.Count > 0);
        }
    }

    internal void BeforeWeaponUpdate()
    {
        if (_failed || !SemiFunc.IsMasterClientOrSingleplayer())
        {
            RemoveWarningItem();
            _target = null;
            _automaticCycle = false;
            _warningState.Reset();
            return;
        }

        try
        {
            bool controls = OwnsAim;
            _target = controls ? FindNearestTarget() : null;
            ItemCartCannonMain.state state = VanillaWeapon.State(_weapon);
            if (_warningState.Observe(controls, _target != null, Time.time))
            {
                RemoveWarningItem();
                _warningItem = MineWarningCarrier.Create(_weapon.transform);
            }
            if (!controls || !_warningState.KeepItem)
            {
                RemoveWarningItem();
            }
            else
            {
                _warningItem?.RenewLease();
            }

            if (_automaticCycle)
            {
                if (state == ItemCartCannonMain.state.active || state == ItemCartCannonMain.state.inactive)
                {
                    _automaticCycle = false;
                }
                else if (!controls || _target == null ||
                    (state == ItemCartCannonMain.state.shooting && !CurrentRayHits(_target)))
                {
                    int? stop = TurretRules.StopState((int)state);
                    if (stop.HasValue)
                    {
                        VanillaWeaponState.Set(_weapon, stop.Value);
                    }
                    return;
                }
                else if (state == ItemCartCannonMain.state.buildup)
                {
                    // Native buildup may be shorter or longer than the requested
                    // first-shot warning. Host authority releases the shot at two
                    // seconds only after the actual barrel is aligned and clear.
                    if (PrepareAutomaticShot())
                    {
                        VanillaWeaponState.Set(_weapon, (int)ItemCartCannonMain.state.shooting);
                    }
                    return;
                }
            }

            // Tracking and native preparation start with detection. Actual fire
            // is gated independently so neither a short native timer nor a late
            // physical turn can fire before the two-second warning has elapsed.
            if (controls && state == ItemCartCannonMain.state.active &&
                _target != null)
            {
                _automaticCycle = true;
                _cycleReadyAt = _warningState.FirstShotPending
                    ? Time.time : Time.time + _weapon.shootBuildUpTime;
                VanillaWeaponState.Set(_weapon, (int)ItemCartCannonMain.state.buildup);
            }
        }
        catch (Exception exception)
        {
            DisableAutomation(exception);
        }
    }

    internal bool PrepareAutomaticShot()
    {
        if (!_automaticCycle)
        {
            return true;
        }
        if (!OwnsAim || _target == null ||
            !_warningState.CanFire(Time.time, _cycleReadyAt, CurrentRayHits(_target)))
        {
            return false;
        }
        // Send network destruction before the stock shooting-state RPC.
        RemoveWarningItem();
        _warningState.MarkFired();
        return true;
    }

    private void RemoveWarningItem()
    {
        if (_warningItem != null)
        {
            _warningItem.Dispose();
            _warningItem = null;
        }
    }

    internal void DisableAutomation(Exception exception)
    {
        _failed = true;
        Plugin.ModLogger.LogError($"Turret disabled on {name}; vanilla behavior retained: {exception}");
        try
        {
            StopAutomaticCycle();
        }
        catch (Exception stopException)
        {
            _automaticCycle = false;
            _target = null;
            Plugin.ModLogger.LogError($"Could not cancel the automatic firing cycle: {stopException}");
        }
    }

    internal void BeforeVanillaGrab()
    {
        if (!OwnsAim || _target == null || _weapon.currentCorrector == null)
        {
            return;
        }

        Enemy enemy = VanillaWeapon.Enemy(_target);
        if (enemy == null || !IsLive(_target, enemy))
        {
            return;
        }

        Transform corrector = _weapon.currentCorrector.transform;
        Quaternion baseline = corrector.rotation;
        if (!TryAimYaw(AimPoint(enemy), _muzzle.position, _muzzle.forward, out float angle))
        {
            return;
        }
        // Use the actual barrel to solve heading, then move the native goal
        // toward that absolute heading. Preserve its upright stabilization.
        float desiredHeading = transform.eulerAngles.y + angle;
        float step = Mathf.Clamp(Mathf.DeltaAngle(baseline.eulerAngles.y, desiredHeading),
            -TurretRules.TurnSpeedDegrees * Time.fixedDeltaTime,
            TurretRules.TurnSpeedDegrees * Time.fixedDeltaTime);
        corrector.rotation = Quaternion.AngleAxis(step, Vector3.up) * baseline;
    }

    private EnemyParent? FindNearestTarget()
    {
        if (EnemyDirector.instance == null)
        {
            return null;
        }

        EnemyParent? nearest = null;
        float bestDistance = float.PositiveInfinity;
        int bestId = int.MaxValue;
        foreach (EnemyParent parent in EnemyDirector.instance.enemiesSpawned)
        {
            if (parent == null)
            {
                continue;
            }

            Enemy enemy = VanillaWeapon.Enemy(parent);
            if (!IsLive(parent, enemy))
            {
                continue;
            }

            Vector3 point = AimPoint(enemy);
            float distance = (point - _muzzle.position).sqrMagnitude;
            int id = parent.GetInstanceID();
            if (!TurretRules.InRange(distance, _range) ||
                !TurretRules.PreferCandidate(distance, id, bestDistance, bestId) ||
                !SweptRayHits(parent, point))
            {
                continue;
            }

            nearest = parent;
            bestDistance = distance;
            bestId = id;
        }

        return nearest;
    }

    private static bool IsLive(EnemyParent parent, Enemy enemy)
    {
        if (enemy == null || !VanillaWeapon.Spawned(parent) || !enemy.isActiveAndEnabled)
        {
            return false;
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        return health == null || !VanillaWeapon.Dead(health);
    }

    private static Vector3 AimPoint(Enemy enemy) =>
        enemy.CenterTransform != null ? enemy.CenterTransform.position : enemy.transform.position;

    private bool SweptRayHits(EnemyParent target, Vector3 point)
    {
        // Only sweep the actual barrel around world up. Never synthesize an
        // upright muzzle or compensate pitch/roll for detection.
        Vector3 muzzlePosition = _muzzle.position;
        Vector3 forward = _muzzle.forward;
        if (!TryAimYaw(point, muzzlePosition, forward, out float yaw))
        {
            return false;
        }

        Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up);
        // Rotate the muzzle offset too; it need not sit on the rigidbody pivot.
        Vector3 origin = transform.position + rotation * (muzzlePosition - transform.position);
        return RayHits(target, origin, rotation * forward);
    }

    private bool TryAimYaw(Vector3 point, Vector3 origin, Vector3 forward, out float yaw)
    {
        Vector3 offset = point - transform.position;
        Vector3 muzzleOffset = origin - transform.position;
        return TurretRules.TryAimYaw(forward.x, forward.z, muzzleOffset.x, muzzleOffset.z,
            offset.x, offset.z, out yaw);
    }

    private bool CurrentRayHits(EnemyParent target) =>
        IsLive(target, VanillaWeapon.Enemy(target)) &&
        TurretRules.InRange((AimPoint(VanillaWeapon.Enemy(target)) - _muzzle.position).sqrMagnitude, _range) &&
        RayHits(target, _muzzle.position, _muzzle.forward);

    private bool RayHits(EnemyParent target, Vector3 origin, Vector3 direction)
    {
        int mask = SemiFunc.LayerMaskGetVisionObstruct() | LayerMask.GetMask("Enemy");
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, _range * TurretRules.DetectionRatio,
            mask, QueryTriggerInteraction.Collide);
        float nearest = float.PositiveInfinity;
        Collider? first = null;
        foreach (RaycastHit hit in hits)
        {
            // The barrel may overlap its own housing. Cart, players, other
            // weapons and scenery still block the real firing corridor.
            if (hit.collider.GetComponentInParent<PhysGrabObject>() == _physical || hit.distance >= nearest ||
                hit.collider.GetComponentInParent<MineWarningCarrier>() != null ||
                (hit.collider.isTrigger && hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy")))
            {
                continue;
            }

            nearest = hit.distance;
            first = hit.collider;
        }

        return first != null && first.GetComponentInParent<EnemyParent>() == target;
    }

    private void StopAutomaticCycle()
    {
        RemoveWarningItem();
        _warningState.Reset();
        if (_automaticCycle && _weapon != null && GameManager.instance != null && SemiFunc.IsMasterClientOrSingleplayer())
        {
            int? stop = TurretRules.StopState((int)VanillaWeapon.State(_weapon));
            if (stop.HasValue)
            {
                VanillaWeaponState.Set(_weapon, stop.Value);
            }
        }

        _automaticCycle = false;
        _target = null;
    }

    private void OnDisable() => StopAutomaticCycle();
}
