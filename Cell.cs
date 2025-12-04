using System;

namespace EZRoomGen
{
    /// <summary>
    /// Defines the triangle winding order for mesh generation.
    /// </summary>
    public enum CellWinding
    {
        Default,
        Flipped,
        DoubleSided
    }
    
    /// <summary>
    /// Represents the four sides of a grid cell.
    /// </summary>
    public enum WallSide
    {
        Left,
        Right,
        Back,
        Front
    }

    /// <summary>
    /// Represents a single cell in the room generation grid.
    /// Contains height information and wall visibility flags for all four sides.
    /// </summary>
    [Serializable]
    public class Cell
    {
        public float height;

        public bool leftWall = true;
        public bool rightWall = true;
        public bool backWall = true;
        public bool frontWall = true;
    }
}