using System;
using UnityEngine;

namespace EZRoomGen.Generation
{
    /// <summary>
    /// Settings used for dungeon / maze generation.
    /// </summary>
    [Serializable]
    public class DungeonLayoutGeneratorSettings : LayoutGeneratorSettings
    {
        [Tooltip("Density of the maze (0-1). Higher = more walls")]
        [Range(0f, 1f)]
        public float density = 0.4f;

        [Tooltip("Number of iterations for maze generation")]
        [Range(1, 10)]
        public int iterations = 8;

        [Tooltip("Minimum path width")]
        [Range(1, 5)]
        public int pathWidth = 1;

        [Tooltip("Smooth the maze edges")]
        public bool smoothEdges = false;
    }
}