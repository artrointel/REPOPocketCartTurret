# PocketCartTurret test plan

## Automated checks

`tests/PocketCartTurret.Checks` checks authority, mounting, power, range boundaries, nearest-target selection, 360-degree yaw with muzzle offsets and fixed pitch, interruption handling, and installed-game field/patch/range contracts. It compares Debug and Release DLLs to verify test spawning is absent from Release.

Physics raycasts, rigidbody motion, in-game event ordering, and network presentation require the manual gameplay checks below.

## Debug spawning

1. Build and deploy Debug, then start a normal mission. One additional pocket cart, cannon, and laser must appear near the truck.
2. Confirm one of each appears in the next mission, but none in shops or lobbies.
3. Client connections and repeated generation-complete events must not create a second set in the same stage.
4. Place the spawned weapons in the cart manually for the remaining checks.

## Activation, power, and movement

1. Cannons and lasers must always preserve native upright stabilization inside carts, including invulnerability, no target, aiming, preparation, firing, recovery, battery depletion, manual control, and automation errors. Do not use prone-pose detection or virtual muzzle correction.
2. A cart without a weapon, or a weapon on the floor or in a large cart, must not fire automatically.
3. A weapon placed in a pocket cart must aim at nearby eligible enemies. Removing it must stop automatic preparation and firing.
4. Operate only with remaining power. Stop after the last battery charge and resume after charging.
5. Cannons consume one native battery bar per shot; lasers consume one during native recovery. Visibility loss must not cause duplicate consumption.
6. The host and clients must be able to pull the cart during automation. Only weapon yaw tracks targets; the cart must not be forcibly locked or rotated.
7. Park the cart and release all handles. It must continue detecting, tracking, and firing as an unattended turret.
8. Directly grabbing the weapon pauses automation and permits manual control. Releasing it inside the cart resumes automation.

## Detection, aiming, and obstacles

1. Check enemies just inside and outside the cannon's 17.5 m and laser's 10.5 m ranges.
2. Verify yaw tracking in front, behind, left, right, and intermediate directions.
3. Enemies above or below, or outside the horizontal firing ray, must not cause vertical aiming or invalid firing.
4. With two living enemies at the same height, select only the nearer eligible enemy. Switch when their distance order reverses.
5. Do not aim at dead or despawned enemies.
6. Walls, doors, objects, players, other weapons, and carts between the muzzle and target must block automatic firing.
7. Do not fire before rotation aligns the actual muzzle. The weapon's own housing must not block detection.
8. Moving behind cover or out of range during preparation/firing must interrupt it. Aim and fire again after requirements recover.

## Multiplayer

1. Install the mod only on the host and connect a vanilla client. Debug equipment must appear at matching locations for everyone.
2. Compare cannon and laser aiming, preparation, firing, recovery, audio, batteries, and enemy damage on host and client screens.
3. A vanilla client must be able to pull the cart, grab the weapon, and remove it with consistent results on both sides.
4. A modded client joining a vanilla host must not enable automation or test spawning.
5. The host remains fixed throughout the session; host migration is outside the game's behavior and is not a test requirement.

## Release

1. Pass the Release build and automated checks. `DebugStageSpawner` and `DebugStageStartPatch` must be absent from the Release DLL.
2. Replace the development profile's Debug DLL with Release and start a normal mission. No test equipment may spawn.
3. Normally purchased pocket carts, cannons, and lasers must retain turret functionality in Release.
4. Packaging with `-Configuration Debug` must be rejected. The Release ZIP must not contain stock game DLLs or test tools.

## Explosive Mine warning and first-shot delay

1. In singleplayer and host-only multiplayer, first detection must spawn one Explosive Mine at the mounted weapon and repeat installation-process beeps. Tracking must start immediately even when the weapon faces away.
2. Neither cannon nor laser may fire before two seconds. Once aligned and unobstructed, the first shot must occur after the hold and the mine must disappear on all clients.
3. If alignment is incomplete after two seconds, continue tracking and waiting while preserving native stabilization. Keep the warning audible even beyond five seconds. Only unmanaged mines expire after five seconds.
4. Continuous shots and nearest-target switches must not restart the warning. Follow-up preparation and battery use must remain native.
5. Brief occlusion must not duplicate the pending warning mine. Any tracking loss resets the two-second hold; loss of at least 0.5 seconds must also remove the mine and start a new mine/warning on reacquisition.
6. Target loss, unmounting, battery depletion, directly grabbing the weapon, and scene changes must not leave temporary mines behind. Normal owned mines and purchase/battery saved state must be unaffected.
7. Pull the cart and verify the mine follows without interfering with cart/weapon stabilization, movement, or aiming. Vanilla-client grab/use attempts must not acquire the mine or cause explosions or damage.
8. Release must spawn warning mines while excluding the Debug-only cart/cannon/laser equipment set.
9. Use gameplay or a recording to verify two seconds of continuous tracking before the first shot. Track for 1.9 seconds, briefly occlude the enemy, and reacquire: another full two seconds must pass. The mine must match the moving/rotating weapon's `transform.position`.
10. Players, enemies, objects, and fast cart movement must not trigger explosions, damage, or arm/disarm transitions. Inventory equip attempts must fail. Normal purchased mines must still work.
11. Join during a warning and verify the arming audio and interaction restrictions are restored. Joining after removal must not replay the warning or resurrect the mine.
12. Check whether a warning mine's local colliders obstruct a vanilla client's interactions with the weapon or cart.
13. Profile frame delays and garbage collection with multiple carts and many enemies. Check unusual muzzle positions inside walls for unintended firing.
