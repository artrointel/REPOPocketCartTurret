# MiniTurret

Turns vanilla mini-cart cannons and lasers into automatic turrets in R.E.P.O.

Place a cart cannon or cart laser in a mini cart during a mission. While powered, it tracks the nearest visible enemy using horizontal rotation only, and fires once the actual barrel has a clear shot.

- Detection radius: 17.5m for the cannon, 10.5m for the laser (70% of vanilla range).
- On detection, a temporary vanilla Explosive Mine at the mounted weapon position repeats its arming beeps at the native cadence while tracking begins. The first shot waits for two seconds of continuous tracking and a clear aligned barrel; the mine is removed before firing.
- Vanilla battery costs, damage, and firing effects.
- Keep pulling the cart normally. Grabbing the weapon itself pauses automation for manual control.
- Install on the host. Other players can use unmodified clients.
- Release packages contain no development item-spawning code.

Requires BepInExPack. Successful plugin initialization writes `MiniTurret v0.1.0 loaded.` to the BepInEx log.

The included icon is a development placeholder.

Warning mines cannot trigger or explode. Ordinary purchased mines retain their behavior.

Even brief tracking loss restarts the two-second hold on reacquisition. A pending warning mine is reused through losses shorter than 0.5 seconds. Continuous follow-up fire retains vanilla preparation times.
