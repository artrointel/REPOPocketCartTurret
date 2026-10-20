# PocketCartTurret

**Turn your pocket cart into an automatic turret. Park it to defend a spot, or take it with you.**

Drop a **Cart Cannon** or **Cart Laser** into a pocket cart and let it watch your back. Park the cart wherever you need an unattended turret, or pull it along for mobile fire support. PocketCartTurret automatically detects nearby enemies, turns the weapon toward the closest clear target, and opens fire whether the cart is stationary or moving.

**Only the host needs the mod. Your friends can play completely vanilla.**

## Host-only installation, shared firepower

Install PocketCartTurret on the host and everyone in the lobby can enjoy the automatic turret, including players without this mod or BepInEx. Weapon rotation, warning beeps, firing effects, and damage use the game's existing multiplayer synchronization.

No extra setup or downloads are required for your friends. Installing the mod only on a client does not enable automatic turrets; it must be installed on the host.

## How to use

1. Install PocketCartTurret and its BepInExPack dependency on the host.
2. Bring a pocket cart and a Cart Cannon or Cart Laser into a mission.
3. Place the weapon in the pocket cart and keep its battery charged.
4. Park the cart at a position you want to defend and let it track and fire on its own. You can also pull it along while it operates.

Grab the weapon itself whenever you want manual control. Release it in the pocket cart to resume automation.

## Features

- **Cannon and laser support:** turn the weapons you already own into automatic turrets.
- **Automatic targeting:** tracks the nearest enemy with a clear firing path within 70% of the weapon's normal range—17.5 m for cannons and 10.5 m for lasers.
- **Warning before firing:** arming beeps play while the turret tracks. The first shot requires two seconds of continuous tracking and an aligned, unobstructed barrel.
- **Stationary or mobile defense:** leave the cart in place as an unattended turret, or pull it along while it tracks and fires. No player needs to hold or push the cart for automation to work.
- **Vanilla weapon behavior:** keeps normal battery consumption, damage, and firing effects. Automation pauses when the weapon is grabbed, removed from the pocket cart, or out of power.

Brief loss of detection restarts the two-second tracking hold. Uninterrupted follow-up shots use the weapon's normal preparation time. Weapons on the floor or in a large cart do not become automatic turrets.

## Installation

Use a Thunderstore-compatible mod manager and install this package on the **host**. Its BepInExPack dependency will be installed automatically.

For manual installation, install BepInExPack first, then extract this ZIP's `BepInEx` folder into your R.E.P.O. game directory. The plugin is `BepInEx/plugins/PocketCartTurret/PocketCartTurret.dll`.

## Links

[Source code and issues](https://github.com/artrointel/REPOPocketCartTurret)
