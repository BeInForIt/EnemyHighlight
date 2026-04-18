# EnemyHighlight

## Game

Barotrauma

## Description

Client-side overlay that highlights eligible characters: non-humans always, humans only when their team differs from the controlled character's. Draws corner boxes, health bar, name, distance when on-screen; off-screen targets use the game's indicator arrow. F6 toggles ESP; toggle state is shown with a short on-screen message.

## Features

- **F6**: Toggle highlight on/off.
- **Filtering**: Excludes self, removed, and dead characters. Crew on same team are not highlighted as enemies.
- **On-screen**: Corner frame, vitality bar with percentage, display name, distance in world units.
- **Off-screen**: Directional indicator using `GUIStyle.EnemyIcon` or `GUI.Arrow`.

## Installation

Copy the mod folder into Barotrauma's local mods directory. Enable the mod in Content Packages.

## Compatibility

- Mod version: 1.0.0
- Patches `GameScreen.Draw`; may conflict with other overlay mods.

## Dependencies

- Barotrauma
- HarmonyLib
- Microsoft.Xna.Framework

## Technical Notes

- Client-only: `CSharp/Client/` assembly.
