using System;

namespace MiniTurret;

// Pure rules also exercised by the standalone checks in tests/MiniTurret.Checks.
internal static class TurretRules
{
    // These ranges are constants in the vanilla ShootLogic/StateShooting methods.
    internal const float CannonRange = 25f;
    internal const float LaserRange = 15f;
    internal const float DetectionRatio = 0.7f;
    internal const float TurnSpeedDegrees = 180f;

    internal static bool CanAutomate(bool host, bool missionReady, bool smallCart,
        bool mounted, bool powered, bool weaponHeld) =>
        host && missionReady && smallCart && mounted && powered && !weaponHeld;

    internal static bool InRange(float distanceSquared, float weaponRange) =>
        distanceSquared >= 0f && distanceSquared <= weaponRange * weaponRange * DetectionRatio * DetectionRatio;

    internal static bool PreferCandidate(float distanceSquared, int id, float bestDistanceSquared, int bestId) =>
        distanceSquared < bestDistanceSquared || (distanceSquared == bestDistanceSquared && id < bestId);

    internal static float YawDelta(float forwardX, float forwardZ, float offsetX, float offsetZ)
    {
        double cross = forwardZ * offsetX - forwardX * offsetZ;
        double dot = forwardX * offsetX + forwardZ * offsetZ;
        return (float)(Math.Atan2(cross, dot) * (180.0 / Math.PI));
    }

    internal static bool TryAimYaw(float forwardX, float forwardZ, float muzzleX, float muzzleZ,
        float targetX, float targetZ, out float yaw)
    {
        yaw = 0f;
        double forwardLength = Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
        double targetLength = Math.Sqrt(targetX * targetX + targetZ * targetZ);
        if (forwardLength < 0.0001 || targetLength < 0.0001)
        {
            return false;
        }

        double lateralOffset = (forwardZ * muzzleX - forwardX * muzzleZ) / forwardLength;
        if (Math.Abs(lateralOffset) >= targetLength)
        {
            return false;
        }

        yaw = YawDelta(forwardX, forwardZ, targetX, targetZ) -
            (float)(Math.Asin(lateralOffset / targetLength) * (180.0 / Math.PI));
        // Ensure that the solution lies in front of the barrel, not behind it.
        double forwardOffset = (forwardX * muzzleX + forwardZ * muzzleZ) / forwardLength;
        return Math.Sqrt(targetLength * targetLength - lateralOffset * lateralOffset) > forwardOffset;
    }

    // No target during buildup: cancel without spending a shot. Once shooting,
    // use goingBack so the vanilla laser spends its normal battery bar exactly once.
    internal static int? StopState(int state) => state == 2 ? 1 : state == 3 ? 4 : null;
}
