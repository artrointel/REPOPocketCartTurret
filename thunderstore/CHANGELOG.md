# Changelog

## 0.1.0

- Keep pending warning mines through brief detection loss, defer arming audio until native initialization, and retain warning audio throughout long tracking/alignment waits.

- Add a temporary vanilla Explosive Mine with repeating arming beeps at first detection, a two-second continuous-tracking hold before the first shot, and network cleanup before firing. Stock clients receive the native Arming state RPC. Warning mines cannot arm, trigger, or explode.

- Preserve vanilla upright stabilization and damping while steering the cart weapon's heading.
- Preserve upright posture on the cart and acquire targets only from the actual muzzle with a yaw-only sweep; never synthesize a corrected barrel pose.
- Ignore non-enemy trigger volumes in firing visibility checks.
- Add host-controlled automatic cannon and laser targeting on mini carts.
- Restrict detection to 70% of vanilla weapon range and a clear, reachable horizontal firing ray.
- Track the nearest live enemy without controlling pitch or restricting cart pulling.
- Reuse vanilla firing, battery, damage, effects, and network synchronization.
- Add Debug-only mission-start test equipment; exclude it from Release and require Release packaging.

- Spawn and follow warning mines at the mounted weapon position. Restart the two-second tracking hold after any tracking loss, while reusing a pending mine through brief loss.

- Remove experimental audio/visual diagnostics, prefab probes, and the F8 sound test. Keep only useful lifecycle/error logs and Debug mission equipment.
- Replay initial warning settings to late joiners and block inventory equip requests.
