# PocketCartTurret feature specification

The host automatically aims and fires stock cannons and lasers mounted in pocket carts. A cart can defend a position unattended or provide fire support while being pulled.

## Activation requirements

1. A normal mission must have finished generating.
2. Decisions run only on the host or in singleplayer. The game uses a fixed host.
3. Native `PhysGrabObjectImpactDetector.inCart/currentCart` must identify a pocket cart.
4. Both remaining battery charge and available battery bars must be positive.
5. Directly grabbing the weapon gives native manual control priority. Pulling the cart is allowed.

## Detection, tracking, and firing

- Cannon maximum range: 25 m; detection radius: 17.5 m.
- Laser maximum range: 15 m; detection radius: 10.5 m.
- Only spawned, active, living enemies are eligible.
- Range is the three-dimensional distance from the muzzle to the enemy center.
- Preserve native upright stabilization in every cart interaction path. Do not allow a prone pose as part of automation or substitute a virtual upright muzzle for a fallen weapon.
- Calculate the firing ray across 360 degrees of yaw using the actual muzzle position, direction, and offset. Do not correct pitch, roll, or height. The ray must reach the enemy collider for detection.
- Walls, objects, carts, and players block targeting. Exclude only the firing weapon's own housing.
- Select the nearest eligible enemy every frame. Resolve equal distances by stable instance order.
- Always execute native `GrabLogic` to preserve stabilization, angular damping, and manual control. Update only the yaw of `currentCorrector`, at up to 180 degrees per second, without adding physics torque. Do not change cart position, rotation, handles, or mass, or aim pitch/roll toward enemies.
- Once the actual muzzle ray reaches the target, use native `buildup → shooting → goingBack → active` states, subject to the warning timer below.
- Cancel preparation if target, visibility, power, or mounting requirements fail. Stop active firing through native recovery. Do not restart recovery and double-charge laser battery usage.

## Vanilla-client compatibility

Add no custom network prefabs or RPCs. Existing `ItemCartCannonMain.StateSet` sends native `StateSetRPC`; stock weapons handle effects, damage, and batteries. Rotation uses existing `PhysGrabObject`/Photon synchronization.

Clients perform no authority decisions even if they have the mod installed. Only the host needs the mod. Validate actual vanilla-client behavior with the multiplayer cases in [the test plan](test-plan.md).

## Development spawning and distribution

- Debug builds spawn one pocket cart, cannon, and laser near the truck after normal mission generation.
- Repeated completion events for the same `LevelGenerator` must not duplicate the equipment.
- In multiplayer the host spawns native room-owned items visible to vanilla clients.
- Do not spawn test equipment in shops, lobbies, or on clients.
- Exclude `Debug/` sources from non-Debug compilation and also guard them with `#if DEBUG`.
- Thunderstore packaging accepts only Release builds.

## Explosive Mine detection warning

- Spawn a native Explosive Mine prefab as a room-owned network object at the mounted weapon. Send native `StateSetRPC(Arming)` once. Vanilla clients repeat `StateArming.soundArmingBeep` at the mine position at native intervals. The host invokes the same logic through a `StateArming` reverse patch while blocking actual arming/explosion transitions. Add no custom RPCs or prefabs.
- Start tracking and native preparation immediately. Allow the first shot only after two continuous seconds of detection, actual muzzle alignment, and a clear firing path. The warning timer gates the first shot even when native preparation takes a different duration.
- Separately gate `StateSet` entry into `shooting` so native `FixedUpdate` cannot fire early. Continue stabilization and tracking while waiting.
- Send `PhotonNetwork.Destroy` immediately before the native firing-state RPC.
- Target switches during continuous tracking do not restart the warning. Occlusion shorter than 0.5 seconds reuses the pending mine, but reacquisition always restarts the two-second tracking timer. Follow-up shots use native preparation times.
- Follow the weapon's current position and rotation. Disable host collision, grabbing, and toggling. Exclude the warning item from host `ItemFetchName`/`AddSpawnedItem` registration to preserve saved state and purchase counts.
- Clean up after target loss, unmounting, power loss, manual grabbing, or disabling. Renew the lifetime during pending tracking; only unmanaged mines expire after five seconds.
- Verify vanilla-client audio, position synchronization, and removal order through actual gameplay.
- Stop native `ItemMine.Update` only for warning mines and disable detection flags and triggers. Block `StateSet`, `TriggeredRPC`, `OnTriggered`, `DestroyMine`, and impact destruction. Normal purchased mines are unaffected.
- During weapon movement and rotation, the mine must match the weapon's `transform.position`. After 1.9 seconds of tracking followed by a brief interruption, reacquisition must require another full two seconds.
- Buffer the initial arming and toggle-disable RPCs for late joiners. Destroying the mine clears Photon instantiation and RPC caches.
- Block `RPC_RequestEquip` independently of grabbing. The global `Sound.Play` diagnostics, state logs, prefab probes, and F8 test have been removed.
