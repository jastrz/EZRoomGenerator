using System;

namespace EZRoomGen.Generation
{
    /// <summary>
    /// Settings used for Room Corridor layout generation.
    /// </summary>
    [Serializable]
    public class RoomCorridorLayoutGeneratorSettings : LayoutGeneratorSettings
    {
        public int maxRooms = 10;
        public int minRoomSize = 4;
        public int maxRoomSize = 10;
    }
}