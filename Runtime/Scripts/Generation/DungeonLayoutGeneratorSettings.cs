using System;
using UnityEngine;

namespace EZRoomGen.Generation
{
    /// <summary>
    /// Settings used for dungeon / maze generation.
    /// </summary>
    [Serializable]
    public class DungeonLayoutGeneratorSettings
    {
        [Tooltip("Random seed for generation (0 = random)")]
        public int seed = 0;

        [Tooltip("Density of the maze (0-1). Higher = more walls")]
        [Range(0f, 1f)]
        public float density = 0.4f;

        [Tooltip("Number of iterations for maze generation")]
        [Range(1, 10)]
        public int iterations = 3;

        [Tooltip("Minimum path width")]
        [Range(1, 5)]
        public int pathWidth = 1;

        [Tooltip("Percentage of dead ends to keep (0-1)")]
        [Range(0f, 1f)]
        public float deadEndKeepChance = 0.3f;

        [Tooltip("Number of extra connections to add")]
        [Range(0, 20)]
        public int loopCount = 5;

        [Tooltip("Smooth the maze edges")]
        public bool smoothEdges = false;

        [Tooltip("Use recursive backtracker algorithm instead of cellular")]
        public bool useRecursiveBacktracker = false;

        public float height = 1f;
    }
}