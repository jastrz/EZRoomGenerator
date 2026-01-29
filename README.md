# EZ Room Generator

**Editor-friendly, Lightweight, Grid-Based Procedural Room & Dungeon Generator for Unity**

EZ Room Generator lets you generate and edit simple levels - rooms, dungeons, and mazes using an interactive grid. It automatically generates floor, wall, and roof meshes, adds colliders, and places lights.

## Features

- **Three Grid Layout Generators:**

  - **Room-Corridor** – Connected room layouts with corridors
  - **Dungeon** – Cave-like structures using Cellular Automata
  - **Maze** – Classic maze generation using Recursive Backtracking

- **Realtime Editing** – Modify generated grid layouts or just draw them from scratch.

## Samples

Inspector and example dungeon room

![Inspector](https://i.imgur.com/hduqY4f.gif)

Editable grid (max size 100x100)

![Grid](https://i.imgur.com/Gq8K7Rg.gif)

Rooms and Corridors (80x80)

![Rooms and Corridors](https://i.imgur.com/XfuVDhb.gif)

Custom Room

![FPP](https://imgur.com/tD8SwYV.png)

## Installation

Option 1: Package Manager (Recommended)

1. Open Unity and navigate to Window > Package Manager
2. Click the + button in the top-left corner
3. Select Add package from git URL...
4. Paste the following URL: `https://github.com/jastrz/EZRoomGenerator.git` and install.

Option 2: Manual Download

1. Clone the repository into your Unity project's `Assets` folder.

Finally, add the `RoomGenerator` component to a GameObject in your scene.

## FBX Export

To enable FBX export functionality:

1. Install the **FBX Exporter** package (`com.unity.formats.fbx`) via Package Manager.
2. Add `USE_FBX_EXPORTER` to **Project Settings → Player → Other Settings → Scripting Define Symbols**.

## Notes

- Generates Wall, Floor and Roof as separate meshes with adjustable mesh resolution.
- Works with all pipelines, although sample materials are for URP, so some adjustments might be needed for them to work.
- Contains simple FPP Controller (Player prefab) with a flashlight, of course.
- Rooms can be joined manually by adding passages to form a larger structure.
- Exported .fbx works with Blender (tested with Blender 4.5, wasn't tested with other 3D software).
- Lightbaking after prefab export is highly suggested due to possibly large count of light sources (if used). This might also need recalculating lightmap uvs (set in exported .fbx import settings) and reasigning materials for exported meshes.

---

_Generate procedural dungeons with ease—no complex setup required._
