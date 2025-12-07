# EZRoomGen

**Editor-friendly, Realtime Grid-Based Procedural Room & Dungeon Generator for Unity**

EZRoomGen lets you generate and edit rooms, dungeons, and mazes using an interactive grid. It automatically generates floor, wall, and roof meshes, adds colliders, and places lights.

## Features

- **Three Grid Layout Generators:**

  - **Room-Corridor** – Connected room layouts with corridors
  - **Dungeon** – Cave-like structures using Cellular Automata
  - **Maze** – Classic maze generation using Recursive Backtracking

- **Realtime Editing** – Modify generated grid layouts or just draw them from scratch.

## Samples

Dungeon

![Dungeon](https://i.imgur.com/y70jZhA.gif)

Rooms and Corridors

![Rooms and Corridors](https://i.imgur.com/RuKg3AB.gif)

Custom Room

![FPP](https://imgur.com/tD8SwYV.png)

## Installation

1. Clone the repository into your Unity project's `Assets` folder.
2. Add the `RoomGenerator` component to a GameObject in your scene.

## Optional: FBX Export

To enable FBX export functionality:

1. Install the **FBX Exporter** package (`com.unity.formats.fbx`) via Package Manager.
2. Add `USE_FBX_EXPORTER` to **Project Settings → Player → Other Settings → Scripting Define Symbols**.

## Notes

- Generates Wall, Floor and Roof as separate meshes.
- Works with all pipelines, although sample materials are for URP, so some adjustments might be needed for them to work.
- Contains simple FPP Controller (Player prefab) with a flashlight, of course.
- Rooms can be joined manually by adding passages to form a larger structure.
- Standalone PlayMode support has not been tested yet.

---

_Generate procedural dungeons with ease—no complex setup required._
