using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EZRoomGen.Generation
{
    /// <summary>
    /// Generates dungeons with rooms connected by corridors.
    /// Returns a 2D float grid where values represent floor heights (0 = wall, >0 = floor).
    /// </summary>
    public class RoomCorridorLayoutGenerator : ILayoutGenerator
    {
        private readonly System.Random _random;
        private RoomCorridorLayoutGeneratorSettings _settings;

        public RoomCorridorLayoutGenerator(RoomCorridorLayoutGeneratorSettings settings = null)
        {
            _settings = settings ?? new RoomCorridorLayoutGeneratorSettings();
            _random = new System.Random(_settings.seed);
        }

        public float[,] Generate(int width, int height)
        {
            var rooms = GenerateRooms(width, height);
            var grid = CreateGrid(width, height);

            foreach (var room in rooms)
            {
                CarveRoom(grid, room);
            }

            ConnectRooms(grid, rooms);

            return grid;
        }

        private List<Room> GenerateRooms(int gridWidth, int gridHeight)
        {
            var rooms = new List<Room>();

            // Ensure settings fit within the grid
            _settings.maxRoomSize = Mathf.Clamp(_settings.maxRoomSize, _settings.maxRoomSize, Mathf.Min(gridWidth, gridHeight) - 2);
            _settings.minRoomSize = Mathf.Clamp(_settings.minRoomSize, _settings.minRoomSize, Mathf.Min(gridWidth, gridHeight) - 2);

            for (int i = 0; i < _settings.maxRooms; i++)
            {
                int w = _random.Next(_settings.minRoomSize, _settings.maxRoomSize + 1);
                int h = _random.Next(_settings.minRoomSize, _settings.maxRoomSize + 1);
                int x = _random.Next(1, gridWidth - w - 1);
                int y = _random.Next(1, gridHeight - h - 1);

                var newRoom = new Room(x, y, w, h);

                if (rooms.Any(r => newRoom.Intersects(r)))
                {
                    continue;
                }

                rooms.Add(newRoom);
            }

            return rooms;
        }

        private float[,] CreateGrid(int width, int height)
        {
            var grid = new float[width, height];

            // Initialize all cells as walls
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = 0.0f;
                }
            }

            return grid;
        }

        private void CarveRoom(float[,] grid, Room room)
        {
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    grid[x, y] = _settings.height;
                }
            }
        }

        private void CarveHorizontalCorridor(float[,] grid, int x1, int x2, int y)
        {
            int startX = Math.Min(x1, x2);
            int endX = Math.Max(x1, x2);

            for (int x = startX; x <= endX; x++)
            {
                grid[x, y] = _settings.height;
            }
        }

        private void CarveVerticalCorridor(float[,] grid, int y1, int y2, int x)
        {
            int startY = Math.Min(y1, y2);
            int endY = Math.Max(y1, y2);

            for (int y = startY; y <= endY; y++)
            {
                grid[x, y] = _settings.height;
            }
        }

        private void ConnectRooms(float[,] grid, List<Room> rooms)
        {
            if (rooms.Count == 0) return;

            // Sort rooms by center to create a consistent path
            var sortedRooms = rooms.OrderBy(r => r.Center.x).ThenBy(r => r.Center.y).ToList();

            for (int i = 0; i < sortedRooms.Count - 1; i++)
            {
                var (x1, y1) = sortedRooms[i].Center;
                var (x2, y2) = sortedRooms[i + 1].Center;

                // Random L-shape corridor
                if (_random.NextDouble() < 0.5)
                {
                    // Horizontal then vertical
                    CarveHorizontalCorridor(grid, x1, x2, y1);
                    CarveVerticalCorridor(grid, y1, y2, x2);
                }
                else
                {
                    // Vertical then horizontal
                    CarveVerticalCorridor(grid, y1, y2, x1);
                    CarveHorizontalCorridor(grid, x1, x2, y2);
                }
            }
        }
    }
}