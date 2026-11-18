# Steel Vanguard

A 3D-physics tower defense game made in Unity. You deploy mechs along a winding route to stop enemy drones, assault mechs and carrier motherships before any of them reach the Federation base.

Made for CSCI 381 Video Game Design and Development, Assignment 2: Tower Defense.

## How to Run

1. Open the project in **Unity 6000.6.9f1**.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**. The game sets itself up when the scene loads, so nothing needs to be wired up in the editor.

## How to Play

| Action | Control |
|---|---|
| Pick a mech | Press **1**, **2** or **3**, or click a mech card at the bottom of the screen |
| Deploy the mech | **Left-click** on open ground. The preview is green where you can deploy and red where you can't. |
| Cancel placement | **Right-click** or **Esc** |
| Pan the camera | **WASD** or the arrow keys |
| Rotate the camera | **Q** / **E** |
| Zoom | Mouse scroll wheel |
| Launch the next wave early | **Space** |
| Toggle 2x speed | **F** |
| Restart (after a win or loss) | **R** |

You start with 6 mech deployments and earn 1 more for every wave you clear. Mechs can't be placed on the enemy route. Once deployed, mechs aim and fire on their own. **If any enemy reaches the base, you lose.** Survive all 8 waves to win.

## Mechs (Towers)

| Mech | Role | Fire rate | Shell speed | Height | Range | Damage |
|---|---|---|---|---|---|---|
| **Striker** | Assault rifle frame | 2.5 shots/s | 34 m/s | 4.5 m | 22 m | 9 |
| **Longshot** | Sniper on a watchtower | 0.7 shots/s | 70 m/s | 10 m | 40 m | 45 |
| **Bastion** | Heavy artillery | 0.4 shots/s | 26 m/s | 3 m | 30 m | 32 splash |

The Bastion lobs its shells on a high arc and damages every enemy within the blast radius.

## Enemies

| Enemy | HP | Speed | Notes |
|---|---|---|---|
| **Scout Drone** | 30 | 7.5 m/s | Fast hover drone |
| **Assault Mech** | 100 | 4.5 m/s | Walking infantry mech |
| **Juggernaut** | 320 | 2.6 m/s | Slow, heavily armored mech |
| **Carrier Mothership** | 1100 | 2.2 m/s | Flying boss that launches Scout Drones as it moves |

## Assignment Requirements

### C
- **The player can place towers.** Pick a mech type and left-click on the ground (`TowerPlacer.cs`).
- **Towers fire continuously at a set rate.** Each mech type has its own fire rate and fires whenever a target is in range (`MechTower.cs`).
- **Towers aim directly at an enemy.** Each mech targets the enemy furthest along the route, turns to face it and fires at it.
- **The game ends when an enemy completes its path.** An enemy reaching the base triggers the "Base Overrun" screen (`GameManager.OnEnemyReachedBase`).

### B
- **Two or more tower types with different rate of fire, projectile speed and tower height.** Striker, Longshot and Bastion all differ in each of these (`Defs.cs`).
- **A text overlay clearly explains how to place towers.** The "How to Deploy Mechs" panel is always on screen, and a live hint shows why a spot is invalid, such as "Can't deploy on the enemy route" (`GameHud.cs`).
- **The number of towers is limited.** The HUD shows mechs deployed out of the current limit (`GameManager.TowerLimit`).
- **Enemy paths are not straight lines.** The route has 10 segments with 9 turns (`EnemyPath.cs`).

### A
- **Multiple enemy types with different speed and HP.** Four enemy types are listed above (`Defs.cs`).
- **Non-basic assets.** The game uses imported 3D models for the drones, mothership, jump gate, hangar base and scenery. The mechs are built in code from multiple parts, with a walking animation.
- **Towers aim at the target's predicted location based on its current velocity and position.** Shells are physics Rigidbodies affected by gravity. For each shot, the tower solves for the launch velocity of a ballistic arc. It then repeatedly predicts where the target will be when the shell arrives (`position + velocity × flight time`) and re-solves until the aim point settles. The aim also corrects for the small extra drop caused by Unity's physics timestep. See `Ballistics.TryLeadTarget`.

### Extras
- 8 waves with a victory screen, score and kill tracking
- Carrier motherships that launch drones mid-flight
- Splash damage and high-arc artillery
- Placement preview showing the mech's range circle
- Health bars, explosions, muzzle flashes and shell trails
- 2x speed, launching waves early, restart
- Strategy camera with pan, rotate and zoom

## Project Structure

```
Assets/MechTD/Scripts/
  GameBootstrap.cs   Starts the game when the scene loads
  GameManager.cs     Battlefield, waves, deployment limits, win/lose state
  Defs.cs            Tower and enemy stats
  EnemyPath.cs       Waypoint route enemies follow
  Enemy.cs           Enemy movement, health, death; mothership drone launches
  MechTower.cs       Mech visuals, targeting and firing
  Ballistics.cs      Ballistic launch solver and predictive aim
  Shell.cs           Physics projectile, hit and splash damage
  TowerPlacer.cs     Placement input and preview
  GameHud.cs         On-screen interface
  Visuals.cs         Materials, model loading, mech builder, effects
  HealthBar.cs, FxBurst.cs, MechWalker.cs, Spinner.cs   Small visual helpers
Assets/MainCamera.cs  Strategy camera
Assets/MechTD/Resources/MechTD/Models/   Imported 3D models
```

## Asset Credits

3D models come from **Kenney** (www.kenney.nl): *Space Kit* and *Tower Defense Kit*. They are licensed under [Creative Commons Zero (CC0)](http://creativecommons.org/publicdomain/zero/1.0/) and are free to use in any project. See `Assets/MechTD/Resources/MechTD/Models/ASSET_CREDITS.txt`.
