# EnemyHighlight

## Game

Barotrauma

## Description

Client-side ESP overlay that draws rectangles around characters (enemies and humans). Toggle with F6. Intended to highlight hostile and friendly NPCs for visibility.

## Features

- **Character indicators**: Box outlines around non-removed, non-dead characters.
- **Color coding**: Red for non-human (enemies), cyan for humans.
- **Distance-based size**: Marker size scales inversely with distance (clamped 12–40px).
- F6 toggles ESP on/off.
- Debug marker in corner (green when active, yellow when no characters, magenta when highlighting but not drawing).

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
- Highlights all characters (including crew); non-humans shown in red, humans in cyan.
