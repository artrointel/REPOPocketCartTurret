# R.E.P.O. 0.5.0 compatibility review — 2026-10-31

Target: PocketCartTurret 0.1.2; installed R.E.P.O. 0.5.0, Steam build 26294889, Unity 6000.3.17.
Official patch notes: https://steamcommunity.com/app/3241660/announcements

## Findings

- Native cannon/laser state values, state RPCs, actual muzzle fields, corrector/GrabLogic stabilization, and 25 m / 15 m maximum ranges remain compatible.
- Native battery use still goes through one RemoveFullBar call per cannon shot or laser recovery. Preserve that path, including the updated native battery/charger behavior.
- The warning mine still accepts native Arming state and toggle-disable RPCs. StateArming retains its installation-process beeps, while the host transition guard prevents real arming or detonation. The new shopkeeper reaction inside StateArming depends on lastPlayerGrabbing; warning props are never player-held. No additional network messages, custom RPCs, or PhotonStream code are needed.
- Unlike RaiseToUpgrade's feedback carrier, this mod spawns the warning at the weapon position and follows it using native physics transform synchronization. It does not use SetPositionRPC as a remote effect-positioning command. Keep the existing network path.
- R.E.P.O. 0.5.0 can defer GenerateDone until local setup is ready. Debug spawning must hook GenerateDoneApply, which actually sets Generated, to avoid missing equipment when the earlier RPC returns before completion. Release continues to exclude the entire Debug source directory.
- The game now destroys pocket carts in pits. Eligibility now explicitly rejects dead/disabled weapons and carts, so tracking/firing and warning retention stop before delayed object removal finishes.
- Add initialization rollback and shot-gate exception containment so compatibility failures do not leave partially installed hooks or escape into the native state machine.
- Use installed game/Photon/Unity assemblies for normal local builds. The published game-reference NuGet package still stops at 0.4.4 and is retained only as an optional fallback. Installed files are never publicized in place or distributed.

## Validation and limits

Debug and Release compile against installed 0.5.0/Unity 6 assemblies with no warnings or errors. All 2,342 checks pass, including complete referenced game/Photon/Unity member resolution, exact Harmony target signatures, reverse-patch contracts, native battery calls and stabilization states, pure targeting/timing rules, equipment guards, and Debug exclusion from Release.

This update has not been launched or tested in gameplay. Manual verification remains necessary for physics, pit destruction, charging, delayed mission startup, and host/unmodded-client audio/effects. The native network path has been preserved; the build and API checks do not replace that gameplay evidence. No installed test profile or running game was changed as part of this update.

Assembly-CSharp.dll SHA256: AA6B1CE0DEEF8D2B7104A3722864C4BA94C107257726471F8E44B45C99E36888
