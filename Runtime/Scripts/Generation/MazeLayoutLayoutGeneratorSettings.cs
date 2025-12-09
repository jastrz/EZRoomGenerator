using System;

namespace EZRoomGen.Generation
{
    /// <summary>
    /// Settings used for Maze layout generation.
    /// </summary>
    [Serializable]
    public class MazeLayoutLayoutGeneratorSettings : LayoutGeneratorSettings
    {
        public int loopCount = 5;
        public float deadEndKeepChance = 0.3f;
        public bool smoothEdges = false;

    }
}