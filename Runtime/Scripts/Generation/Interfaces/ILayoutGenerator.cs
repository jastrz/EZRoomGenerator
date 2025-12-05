namespace EZRoomGen.Generation
{

    /// <summary>
    /// Interface for dungeon generators that produce 2D height maps.
    /// </summary>
    public interface ILayoutGenerator
    {
        /// <summary>
        /// Generates a dungeon and returns it as a 2D float array.
        /// </summary>
        /// <param name="width">Width of the dungeon grid.</param>
        /// <param name="height">Height of the dungeon grid.</param>
        /// <returns>2D array where [x,y] contains height values (0.0 for walls, 1.0 for floors).</returns>
        float[,] Generate(int width, int height);
    }

}
