using System;
using UnityEngine;

namespace EZRoomGen
{
    /// <summary>
    /// Class Containg Grid data for procedural room generation
    /// </summary>
    [Serializable]
    public class GridData : ISerializationCallbackReceiver
    {
        // Runtime 2D array (not serialized)
        [NonSerialized]
        public Cell[,] cells;

        // Serialized flat array for Unity
        [SerializeField]
        private Cell[] serializedCells;

        [SerializeField]
        public int gridWidth;

        [SerializeField]
        public int gridHeight;

        public GridData(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            InitializeGrid();
        }
        
        /// <summary>
        /// Creates and initializes the 2D cells array with new Cell instances.
        /// </summary>
        public void InitializeGrid()
        {
            cells = new Cell[gridWidth, gridHeight];
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    cells[x, y] = new Cell();
        }

        public void OnBeforeSerialize()
        {
            if (cells == null || cells.Length == 0)
                return;

            serializedCells = new Cell[gridWidth * gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    serializedCells[y * gridWidth + x] = cells[x, y];
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (serializedCells == null || serializedCells.Length == 0)
                return;

            cells = new Cell[gridWidth, gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int index = y * gridWidth + x;
                    if (index < serializedCells.Length)
                    {
                        cells[x, y] = serializedCells[index];
                    }
                    else
                    {
                        cells[x, y] = new Cell();
                    }
                }
            }
        }

        /// <summary>
        /// Resizes the grid to new dimensions while preserving existing cell data.
        /// Automatically expands dimensions to prevent loss of non-empty cells when shrinking.
        /// </summary>
        public bool ResizeGrid(ref int newWidth, ref int newHeight)
        {
            Cell[,] oldCells = cells;
            int oldWidth = oldCells.GetLength(0);
            int oldHeight = oldCells.GetLength(1);

            // Check if shrinking would lose non-empty cells
            if (newWidth < oldWidth || newHeight < oldHeight)
            {
                // Check the areas that would be removed
                for (int y = 0; y < oldHeight; y++)
                {
                    for (int x = 0; x < oldWidth; x++)
                    {
                        // Check if this cell is outside the new bounds AND has height > 0
                        if (oldCells[x, y].height > 0)
                        {
                            if (x >= newWidth)
                            {
                                newWidth = x + 1;
                            }
                            if (y >= newHeight)
                            {
                                newHeight = y + 1;
                            }
                        }
                    }
                }
            }

            cells = new Cell[newWidth, newHeight];

            for (int y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
                for (int x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
                    cells[x, y] = oldCells[x, y];

            // Initialize new cells if expanding
            for (int y = 0; y < newHeight; y++)
                for (int x = 0; x < newWidth; x++)
                    if (cells[x, y] == null)
                        cells[x, y] = new Cell();

            gridWidth = newWidth;
            gridHeight = newHeight;

            return true;
        }

        /// <summary>
        /// Clears all cell heights in the grid, setting them to 0 while preserving the grid structure.
        /// </summary>
        public void ClearGrid()
        {
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    cells[x, y].height = 0f;
        }

    }
}
