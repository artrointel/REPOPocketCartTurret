# Code review — 2026-10-20

This review assumes the game's host remains fixed.

## Cleanup and fixes

- Removed the global `Sound.Play` diagnostic patch; audio, renderer, detection, and first-shot logs; five-second status output; prefab probes; the F8 test; and the abandoned sound-candidate document. Kept plugin load/error logs and the originally requested Debug stage equipment spawning.
- An absolute five-second mine lifetime could silence the warning before firing during repeated occlusion or long alignment waits. The controller now renews the pending lifetime. The five-second cleanup limit applies only to unmanaged mines.
- Blocked inventory `RPC_RequestEquip` independently of grabbing for warning mines. Normal purchased mines are unaffected.
- Buffered both initial RPCs so late joiners receive arming state and interaction restrictions along with the spawned mine. Confirmed in the installed Photon implementation that `RemoveInstantiatedGO` clears the instantiation cache and calls `OpCleanRpcBuffer` on removal.

## Remaining risks and verification scenarios

| Priority | Scenario | Review findings and required verification |
| --- | --- | --- |
| Medium | A vanilla client grabs above the weapon or presses an inventory key during a warning | Host collision, grab approval, and equip requests are blocked, but the vanilla client's local mine colliders remain. The overlapping mine could obscure local weapon selection or aiming. Verify interactions with an actual vanilla client. |
| Medium | The muzzle overlaps a wall or the cart interior | Visibility uses `Physics.RaycastAll` without a separate starting-point overlap check. Test unusual overlapping poses for unintended firing or detection failure. This is a potential issue, not a confirmed reproduction. |
| Low | Many turrets and enemies are active simultaneously | Candidate iteration and `RaycastAll` result allocations occur each frame. Larger scenes could cause garbage collection or frame delays; performance has not been profiled. |

## Verification scope

Verification covers Debug/Release compilation; pure detection, timing, and aiming rules; inspection of build artifacts for patch/diagnostic removal; and contracts against the installed game DLL. Post-fix multiplayer checks for late joining and vanilla-client interactions still require actual gameplay. Follow [the manual test plan](test-plan.md).
