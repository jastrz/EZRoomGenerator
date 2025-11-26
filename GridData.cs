namespace EZRoomGen
{
    using System;
    using UnityEngine;

    [Serializable]
    public class GridData
    {
        public Cell[,] cells;
        public int gridWidth;
        public int gridHeight;

        public GridData(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            cells = new Cell[gridWidth, gridHeight];
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    cells[x, y] = new Cell(); // Default height can be set in Cell constructor
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            Cell[,] oldCells = cells;
            int oldWidth = oldCells.GetLength(0);
            int oldHeight = oldCells.GetLength(1);

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
        }

        public void ClearGrid()
        {
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    cells[x, y].height = 0f;
        }

        public void FillGrid(float height)
        {
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    cells[x, y].height = height;
        }
    }
}