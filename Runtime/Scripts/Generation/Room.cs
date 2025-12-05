namespace EZRoomGen.Generation
{
    /// <summary>
    /// Represents a rectangular room for the Room Corridor generator.
    /// </summary>
    public class Room
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Room(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public (int x, int y) Center
        {
            get
            {
                int cx = X + Width / 2;
                int cy = Y + Height / 2;
                return (cx, cy);
            }
        }

        public bool Intersects(Room other)
        {
            return !(X + Width < other.X ||
                     X > other.X + other.Width ||
                     Y + Height < other.Y ||
                     Y > other.Y + other.Height);
        }
    }
}