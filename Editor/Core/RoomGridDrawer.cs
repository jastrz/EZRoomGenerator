#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace EZRoomGen.Core.Editor
{
    /// <summary>
    /// Shared helper class for drawing and interacting with the room grid.
    /// Can be used in both the inspector and separate editor windows.
    /// </summary>
    [Serializable]
    public class RoomGridDrawer
    {
        private const float gridPadding = 10f;
        private bool isDragging = false;

        public float CellSize { get; set; } = 30f;
        public const float MinCellSize = 10f;
        public const float MaxCellSize = 60f;

        public void Draw(RoomGenerator generator)
        {
            HandleZoom();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Layout Grid", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            DrawGrid(generator);

            EditorGUILayout.Space();
            DrawSelectedCellControls(generator);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "• Left-click and drag to paint cells\n" +
                "• Right-click to select cell\n" +
                "• Middle-click and drag to erase cells\n" +
                "• Ctrl + Scroll to zoom in/out",
                MessageType.Info);
        }

        /// <summary>
        /// Handles Ctrl+Scroll zoom functionality.
        /// </summary>
        private bool HandleZoom()
        {
            Event e = Event.current;

            if (e.type == EventType.ScrollWheel && e.control)
            {
                float delta = -e.delta.y;
                CellSize = Mathf.Clamp(CellSize + delta * 0.5f, MinCellSize, MaxCellSize);

                e.Use();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Draws the complete grid with all cells and handles input.
        /// </summary>
        private void DrawGrid(RoomGenerator generator)
        {
            if (generator == null) return;

            var gridDimensions = GetGridDimensions(generator);
            var selectedCell = GetSelectedCell(generator);

            float gridWidth = CellSize * gridDimensions.width + gridPadding * 2;

            Rect gridRect = GUILayoutUtility.GetRect(
                CellSize * gridDimensions.width + gridPadding * 2,
                CellSize * gridDimensions.height + gridPadding * 2
            );

            // Center the grid horizontally
            float availableWidth = gridRect.width;
            float xOffset = (availableWidth - gridWidth) / 2f;
            if (xOffset > 0)
            {
                gridRect.x += xOffset;
                gridRect.width = gridWidth;
            }

            Event e = Event.current;

            // Draw each cell
            for (int y = 0; y < gridDimensions.height; y++)
            {
                for (int x = 0; x < gridDimensions.width; x++)
                {
                    Rect cellRect = CalculateCellRect(gridRect, x, y, gridDimensions.height);
                    HandleCellInput(generator, x, y, cellRect, e, ref selectedCell);

                    bool isActive = generator.GetCellHeight(x, y) > 0;
                    bool isSelected = (x == selectedCell.x && y == selectedCell.y);

                    DrawCell(generator, cellRect, x, y, isActive, isSelected);
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
            }
        }

        /// <summary>
        /// Draws controls for the currently selected cell (height, walls).
        /// </summary>
        private void DrawSelectedCellControls(RoomGenerator generator)
        {
            var selectedCell = GetSelectedCell(generator);

            if (selectedCell.x >= 0 && selectedCell.y >= 0)
            {
                EditorGUILayout.LabelField($"Selected Cell: ({selectedCell.x}, {selectedCell.y})", EditorStyles.boldLabel);

                var realtimeGeneration = GetRealtimeGeneration(generator);

                // Setting height disabled for now

                // EditorGUI.BeginChangeCheck();
                // float currentHeight = generator.GetCellHeight(selectedCell.x, selectedCell.y);
                // float newHeight = EditorGUILayout.Slider("Cell Height", currentHeight, 0f, 5f);

                // if (EditorGUI.EndChangeCheck())
                // {
                //     generator.SetCellHeight(selectedCell.x, selectedCell.y, newHeight);
                //     if (realtimeGeneration) generator.GenerateRoom();
                // }

                // Wall toggles - only show if cell has height
                if (generator.GetCellHeight(selectedCell.x, selectedCell.y) > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Walls", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();

                    bool leftWall = generator.GetCellWall(selectedCell.x, selectedCell.y, WallSide.Left);
                    bool rightWall = generator.GetCellWall(selectedCell.x, selectedCell.y, WallSide.Right);
                    bool backWall = generator.GetCellWall(selectedCell.x, selectedCell.y, WallSide.Back);
                    bool frontWall = generator.GetCellWall(selectedCell.x, selectedCell.y, WallSide.Front);

                    leftWall = EditorGUILayout.Toggle("Left Wall (West)", leftWall);
                    rightWall = EditorGUILayout.Toggle("Right Wall (East)", rightWall);
                    backWall = EditorGUILayout.Toggle("Back Wall (South)", backWall);
                    frontWall = EditorGUILayout.Toggle("Front Wall (North)", frontWall);

                    if (EditorGUI.EndChangeCheck())
                    {
                        generator.SetCellWall(selectedCell.x, selectedCell.y, WallSide.Left, leftWall);
                        generator.SetCellWall(selectedCell.x, selectedCell.y, WallSide.Right, rightWall);
                        generator.SetCellWall(selectedCell.x, selectedCell.y, WallSide.Back, backWall);
                        generator.SetCellWall(selectedCell.x, selectedCell.y, WallSide.Front, frontWall);

                        if (realtimeGeneration) generator.GenerateRoom();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Right-click on a cell to select it for editing.", MessageType.Info);
            }
        }

        /// <summary>
        /// Retrieves the grid dimensions from the RoomGenerator .
        /// </summary>
        private (int width, int height) GetGridDimensions(RoomGenerator generator)
        {
            int width = (int)typeof(RoomGenerator).GetField("gridWidth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
            int height = (int)typeof(RoomGenerator).GetField("gridHeight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
            return (width, height);
        }

        /// <summary>
        /// Retrieves the currently selected cell coordinates from the RoomGenerator.
        /// </summary>
        private (int x, int y) GetSelectedCell(RoomGenerator generator)
        {
            var selectedXField = typeof(RoomGenerator).GetField("selectedX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var selectedYField = typeof(RoomGenerator).GetField("selectedY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int x = (int)selectedXField.GetValue(generator);
            int y = (int)selectedYField.GetValue(generator);
            return (x, y);
        }

        /// <summary>
        /// Sets the selected cell coordinates in the RoomGenerator using reflection.
        /// </summary>
        private void SetSelectedCell(RoomGenerator generator, int x, int y)
        {
            var selectedXField = typeof(RoomGenerator).GetField("selectedX",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var selectedYField = typeof(RoomGenerator).GetField("selectedY",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            selectedXField.SetValue(generator, x);
            selectedYField.SetValue(generator, y);
        }

        private bool GetRealtimeGeneration(RoomGenerator generator)
        {
            return (bool)typeof(RoomGenerator).GetField("realtimeGeneration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
        }

        private float GetDefaultHeight(RoomGenerator generator)
        {
            return (float)typeof(RoomGenerator).GetField("defaultHeight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(generator);
        }

        /// <summary>
        /// Calculates the screen-space rectangle for a grid cell.
        /// Y-axis is inverted so that (0,0) is at the bottom-left.
        /// </summary>
        private Rect CalculateCellRect(Rect gridRect, int x, int y, int gridHeight)
        {
            return new Rect(
                gridRect.x + gridPadding + x * CellSize,
                gridRect.y + gridPadding + (gridHeight - 1 - y) * CellSize,
                CellSize - 2,
                CellSize - 2
            );
        }

        /// <summary>
        /// Handles mouse input for a cell: painting, selecting, and erasing.
        /// - Left click/drag: Toggle or paint cells
        /// - Right click: Select cell
        /// - Middle click/drag: Erase cells
        /// </summary>
        private void HandleCellInput(RoomGenerator generator, int x, int y, Rect cellRect, Event e, ref (int x, int y) selectedCell)
        {
            bool isHovering = cellRect.Contains(e.mousePosition);
            if (!isHovering) return;

            var realtimeGeneration = GetRealtimeGeneration(generator);
            var defaultHeight = GetDefaultHeight(generator);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDragging = true;
                generator.ToggleCell(x, y);
                if (realtimeGeneration) generator.GenerateRoom();
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                SetSelectedCell(generator, x, y);
                selectedCell = (x, y);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDragging && e.button == 0)
            {
                if (generator.GetCellHeight(x, y) <= 0)
                {
                    generator.SetCellHeight(x, y, defaultHeight);
                    if (realtimeGeneration) generator.GenerateRoom();
                }
                e.Use();
            }
            else if ((e.type == EventType.MouseDown && e.button == 2) ||
                     (e.type == EventType.MouseDrag && e.button == 2))
            {
                if (generator.GetCellHeight(x, y) > 0)
                {
                    generator.SetCellHeight(x, y, 0f);
                    if (realtimeGeneration) generator.GenerateRoom();
                }
                e.Use();
            }
        }

        /// <summary>
        /// Renders a single grid cell with its color, outline, and height label.
        /// </summary>
        private void DrawCell(RoomGenerator generator, Rect cellRect, int x, int y, bool isActive, bool isSelected)
        {
            Color cellColor = GetCellColor(generator, x, y, isActive, isSelected);

            EditorGUI.DrawRect(cellRect, cellColor);
            Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(0.3f, 0.3f, 0.3f));

            if (isActive)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(8, (int)(CellSize / 3))
                };
                style.normal.textColor = Color.white;

                GUI.Label(cellRect, generator.GetCellHeight(x, y).ToString("F1"), style);
            }
        }


        /// <summary>
        /// Determines the color of a cell based on its state.
        /// </summary>
        private Color GetCellColor(RoomGenerator generator, int x, int y, bool isActive, bool isSelected)
        {
            if (isSelected && isActive)
                return new Color(1f, 0.8f, 0.2f);

            if (isSelected)
                return new Color(0.8f, 0.5f, 0.1f);

            if (isActive)
            {
                float heightRatio = generator.GetCellHeight(x, y) / 5f;
                return Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(0.2f, 1f, 0.4f), heightRatio);
            }

            return new Color(0.2f, 0.2f, 0.2f);
        }
    }
}

#endif