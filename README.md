# PocketCartTurret

**Turn your pocket cart into an automatic turret with a Cart Cannon or Cart Laser.**

Automatically detect, track, and fire at nearby enemies. Park the cart wherever you need unattended defense, or pull it along for mobile fire support. Automation works without anyone holding or pushing the cart.

**Only the host needs the mod. Friends can play completely vanilla, without this mod or BepInEx.** Rotation, warning beeps, firing effects, and damage use the game's existing multiplayer synchronization.

The mod is named **PocketCartTurret** and its repository is **REPOPocketCartTurret**. [GitHub repository](https://github.com/artrointel/REPOPocketCartTurret)

The development setup follows REPORaiseToUpgrade.

## Behavior

- Automates stock cannons and lasers mounted in pocket carts during normal missions. Weapons on the floor or in large carts are excluded.
- Requires battery power. Detection range is 17.5 m for cannons and 10.5 m for lasers: 70% of their stock maximum ranges of 25 m and 15 m.
- Always preserves native upright stabilization in the cart. Tracks the nearest enemy reachable by rotating the actual muzzle horizontally, without synthesizing an upright muzzle pose or aiming vertically.
- Uses native preparation, shooting, and recovery states once the actual muzzle is aligned and unobstructed. Losing the target or firing path interrupts automatic preparation and firing.
- Preserves stock battery consumption, damage, sounds, and effects. Players can still pull the cart normally.
- Grabbing the weapon itself pauses automation for manual control. Pulling the cart handle does not pause automation.
- The host controls detection and rotation through native RPCs and physics synchronization. Clients need no mod; installing it only on a client does not enable automation.

See [the manual test plan](planning/test-plan.md) for in-game physics, obstacle, and vanilla-client checks.

## Debug-only test equipment

Debug builds spawn one pocket cart, one cannon, and one laser near the truck's safe spawn point after each normal mission finishes generating. Weapons spawn separately; place them in the cart manually. No test equipment spawns in the shop or lobby. Purchase lists and owned-item counts are not forcibly changed.

Spawner sources are isolated in `src/PocketCartTurret/Debug/`, guarded by `#if DEBUG`, and excluded from non-Debug compilation. The Release DLL contains neither the spawner nor its stage-start patch. Thunderstore packaging accepts only Release builds.

## Development environment

- .NET 9 SDK, C# 12, `netstandard2.1`
- BepInEx 5.4.21, HarmonyX 2.7.0
- UnityEngine.Modules 2022.3.62
- R.E.P.O.GameLibs.Steam 0.4.4-ngd.0
- Linkoid.Repo.Plugin.Build 0.2.2

BepInEx is pinned to the version restored by the reference project. Local `.dotnet/` SDK and `.nuget/packages/` caches are ignored by Git. On a new PC, install a .NET 9 SDK compatible with `global.json`. VS Code tasks use `.dotnet/dotnet.exe`.

## Build and run

The local `Directory.Repo.props` uses the default Steam installation and the Thunderstore Mod Manager `REPO` profile. Builds deploy the DLL/PDB to `BepInEx/plugins/PocketCartTurret/`.

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:NUGET_PACKAGES = "$PWD\.nuget\packages"
.\.dotnet\dotnet.exe restore src\PocketCartTurret\PocketCartTurret.csproj
.\.dotnet\dotnet.exe build src\PocketCartTurret\PocketCartTurret.csproj --configuration Debug --no-restore
```

To restore using only an existing local package cache:

```powershell
.\.dotnet\dotnet.exe restore src\PocketCartTurret\PocketCartTurret.csproj --source "$PWD\.nuget\packages" -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false
```

Add `-p:CopyFilesToPluginOutputDirectoryOnBuild=false` to build without deploying to the game profile.

- `Ctrl+Shift+B`: build Debug and deploy to the development profile.
- `Tasks: Run Task` → `PocketCartTurret: Start game`: build, deploy, and launch the game.
- `F5` → `PocketCartTurret: Launch and attach`: launch and attach the Mono debugger at `127.0.0.1:10000`.
- `PocketCartTurret: Watch BepInEx log`: follow the development profile log.

F5 requires the VS Code C# Dev Kit and Unity extensions. Close the game before rebuilding and launching. Successful loading writes `PocketCartTurret v0.1.1 loaded.` to the log.

For another profile, copy `Directory.Repo.props.example` or `Directory.Repo.Gale.props.example` to `Directory.Repo.props` and adjust it. Update the launch script and log task paths as well. `Directory.Repo.props` is ignored by Git because it contains local paths.

## Thunderstore package

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Pack-Thunderstore.ps1
```

The output is `artifacts/PocketCartTurret-0.1.1.zip`. Packaging does not deploy to the game profile. The original artwork is `assets/PocketCartTurret.png`; the submission icon is the 256×256 PNG at `thunderstore/icon.png`. Versions must match in the csproj, `Plugin.cs`, and `thunderstore/manifest.json`.

Add `-NoRestore` to package offline after restoring dependencies.

## Automated checks

Build both Debug and Release, then run the checks below. Use the MSBuild property described above to avoid deploying builds to the game profile.

```powershell
.\.dotnet\dotnet.exe restore tests\PocketCartTurret.Checks\PocketCartTurret.Checks.csproj --source "$PWD\.nuget\packages" -p:NuGetAudit=false
.\.dotnet\dotnet.exe run --project tests\PocketCartTurret.Checks\PocketCartTurret.Checks.csproj --no-restore
```

Checks cover authority, mounting and battery conditions, the 70% range boundary, nearest-target selection, 360-degree yaw calculations with muzzle offsets and fixed pitch, interruption transitions, installed-game patch targets/fields/ranges, and exclusion of test spawning from Release. For a non-default Steam installation, append `-- "<path to the game's Assembly-CSharp.dll>"`. These checks do not replace in-game or multiplayer testing.

## Project layout

```text
src/PocketCartTurret/   Plugin source
planning/              Feature specification, code review, and manual tests
scripts/               Game launch and packaging
thunderstore/           Distribution metadata and submission icon
.vscode/                Build, debugging, and log tasks
tests/                  Rules, aiming geometry, game integration, and Release checks
```

## Detection warning

On detection, a temporary stock Explosive Mine spawns at the mounted weapon's `transform.position` and follows its position and rotation. It repeats the native installation-process beeps while tracking and preparation begin. The first shot requires two seconds of continuous tracking, actual muzzle alignment, and a clear firing path. The mine is removed over the network immediately before firing.

Even brief detection loss resets the tracking timer: reacquisition must complete another two seconds. Loss shorter than 0.5 seconds reuses the pending mine; longer loss removes it and creates a new one on reacquisition. Switching to a closer target during continuous tracking does not count as detection loss. Follow-up shots use native weapon preparation times. Losing and reacquiring tracking after the first shot starts a new warning and two-second hold.

Unmounting, battery depletion, directly grabbing the weapon, or disabling automation removes the warning immediately. Pending tracking renews the mine's lifetime; only mines no longer managed by a controller expire after five seconds. Warning mines cannot collide, be grabbed, be toggled, arm, explode, or enter purchase counts, owned-item counts, or saved state. Normal purchased mines retain their behavior.

Routine diagnostics and the F8 probe were removed. Plugin loading/error logs and Debug-only stage equipment spawning remain. See [the code review](planning/code-review.md) for findings and outstanding manual checks.
