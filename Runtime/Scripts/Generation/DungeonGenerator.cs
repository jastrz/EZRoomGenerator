
using System.Collections.Generic;

namespace EZRoomGen.Generation
{
    /// <summary>
    /// Generates dungeon and maze layouts using either Recursive Backtracker or Cellular Automata algorithms.
    /// Returns a 2D float grid where values represent floor heights (0 = wall, >0 = floor).
    /// </summary>
    public class DungeonLayoutGenerator : ILayoutGenerator
    {
        private DungeonLayoutGeneratorSettings _settings;
        private System.Random _random;

        public DungeonLayoutGenerator(DungeonLayoutGeneratorSettings settings = null)
        {
            _settings = settings ?? new DungeonLayoutGeneratorSettings();
        }

        public float[,] Generate(int width, int height)
        {
            _random = new System.Random(_settings.seed);

            float[,] grid;

            if (_settings.useRecursiveBacktracker)
            {
                grid = GenerateRecursiveBacktracker(width, height);
            }
            else
            {
                grid = GenerateCellular(width, height);
            }

            if (_settings.loopCount > 0)
            {
                AddLoops(grid, width, height);
            }

            if (_settings.deadEndKeepChance == 0)
            {
                RemoveDeadEnds(grid, width, height);
            }
            else if (_settings.deadEndKeepChance < 1f)
            {
                RemoveSomeDeadEnds(grid, width, height);
            }

            if (_settings.smoothEdges)
            {
                SmoothEdges(grid, width, height);
            }

            return grid;
        }

        private float[,] GenerateCellular(int width, int height)
        {
            var grid = new float[width, height];

            // Initialize with random walls
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    {
                        grid[x, y] = 0f; // Border walls
                    }
                    else
                    {
                        grid[x, y] = _random.NextDouble() < _settings.density ? 0f : _settings.height;
                    }
                }
            }

            // Cellular automata iterations
            for (int i = 0; i < _settings.iterations; i++)
            {
                grid = CellularStep(grid, width, height);
            }

            // Ensure connectivity
            FloodFillLargestArea(grid, width, height);

            // Widen paths if needed
            if (_settings.pathWidth > 1)
            {
                grid = WidenPaths(grid, width, height);
            }

            return grid;
        }

        private float[,] GenerateRecursiveBacktracker(int width, int height)
        {
            var grid = new float[width, height];

            // Initialize all as walls
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = 0f;
                }
            }

            // Start from center
            int startX = width / 2;
            int startY = height / 2;

            var visited = new bool[width, height];
            var stack = new Stack<(int x, int y)>();

            stack.Push((startX, startY));
            visited[startX, startY] = true;
            grid[startX, startY] = _settings.height;

            var directions = new[] { (0, -2), (2, 0), (0, 2), (-2, 0) };

            while (stack.Count > 0)
            {
                var (x, y) = stack.Peek();
                var neighbors = new List<(int x, int y, int dx, int dy)>();

                foreach (var (dx, dy) in directions)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx > 0 && nx < width - 1 && ny > 0 && ny < height - 1 && !visited[nx, ny])
                    {
                        neighbors.Add((nx, ny, dx, dy));
                    }
                }

                if (neighbors.Count > 0)
                {
                    var chosen = neighbors[_random.Next(neighbors.Count)];
                    int nx = chosen.x;
                    int ny = chosen.y;
                    int wallX = x + chosen.dx / 2;
                    int wallY = y + chosen.dy / 2;

                    grid[wallX, wallY] = _settings.height;
                    grid[nx, ny] = _settings.height;
                    visited[nx, ny] = true;
                    stack.Push((nx, ny));
                }
                else
                {
                    stack.Pop();
                }
            }

            return grid;
        }

        private float[,] CellularStep(float[,] grid, int width, int height)
        {
            var newGrid = new float[width, height];

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    int wallCount = CountWallNeighbors(grid, x, y, width, height);

                    if (wallCount >= 5)
                        newGrid[x, y] = 0f;
                    else if (wallCount <= 3)
                        newGrid[x, y] = _settings.height;
                    else
                        newGrid[x, y] = grid[x, y];
                }
            }

            return newGrid;
        }

        private int CountWallNeighbors(float[,] grid, int x, int y, int width, int height)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || grid[nx, ny] < 0.5f)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private void FloodFillLargestArea(float[,] grid, int width, int height)
        {
            var areas = new List<HashSet<(int, int)>>();
            var visited = new bool[width, height];

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (grid[x, y] > 0.5f && !visited[x, y])
                    {
                        var area = FloodFill(grid, x, y, width, height, visited);
                        areas.Add(area);
                    }
                }
            }

            if (areas.Count == 0) return;

            var largest = areas[0];
            foreach (var area in areas)
            {
                if (area.Count > largest.Count)
                    largest = area;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] > 0.5f && !largest.Contains((x, y)))
                    {
                        grid[x, y] = 0f;
                    }
                }
            }
        }

        private HashSet<(int, int)> FloodFill(float[,] grid, int startX, int startY, int width, int height, bool[,] visited)
        {
            var area = new HashSet<(int, int)>();
            var queue = new Queue<(int, int)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                area.Add((x, y));

                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                        !visited[nx, ny] && grid[nx, ny] > 0.5f)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return area;
        }

        private void AddLoops(float[,] grid, int width, int height)
        {
            int added = 0;
            int attempts = 0;
            int maxAttempts = _settings.loopCount * 10;

            while (added < _settings.loopCount && attempts < maxAttempts)
            {
                attempts++;
                int x = _random.Next(2, width - 2);
                int y = _random.Next(2, height - 2);

                if (grid[x, y] < 0.5f)
                {
                    int floorNeighbors = 0;
                    if (grid[x - 1, y] > 0.5f) floorNeighbors++;
                    if (grid[x + 1, y] > 0.5f) floorNeighbors++;
                    if (grid[x, y - 1] > 0.5f) floorNeighbors++;
                    if (grid[x, y + 1] > 0.5f) floorNeighbors++;

                    if (floorNeighbors >= 2)
                    {
                        grid[x, y] = _settings.height;
                        added++;
                    }
                }
            }
        }

        private void RemoveDeadEnds(float[,] grid, int width, int height)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        if (grid[x, y] > 0.5f && IsDeadEnd(grid, x, y))
                        {
                            grid[x, y] = 0f;
                            changed = true;
                        }
                    }
                }
            }
        }

        private void RemoveSomeDeadEnds(float[,] grid, int width, int height)
        {
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (grid[x, y] > 0.5f && IsDeadEnd(grid, x, y))
                    {
                        if (_random.NextDouble() > _settings.deadEndKeepChance)
                        {
                            grid[x, y] = 0f;
                        }
                    }
                }
            }
        }

        private bool IsDeadEnd(float[,] grid, int x, int y)
        {
            int openSides = 0;
            if (grid[x - 1, y] > 0.5f) openSides++;
            if (grid[x + 1, y] > 0.5f) openSides++;
            if (grid[x, y - 1] > 0.5f) openSides++;
            if (grid[x, y + 1] > 0.5f) openSides++;
            return openSides == 1;
        }

        private float[,] WidenPaths(float[,] grid, int width, int height)
        {
            var newGrid = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newGrid[x, y] = grid[x, y];
                }
            }

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (grid[x, y] > 0.5f)
                    {
                        for (int dx = 0; dx < _settings.pathWidth; dx++)
                        {
                            for (int dy = 0; dy < _settings.pathWidth; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx < width - 1 && ny < height - 1)
                                {
                                    newGrid[nx, ny] = _settings.height;
                                }
                            }
                        }
                    }
                }
            }

            return newGrid;
        }

        private void SmoothEdges(float[,] grid, int width, int height)
        {
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (grid[x, y] < 0.5f)
                    {
                        int floorCount = 0;
                        if (grid[x - 1, y] > 0.5f) floorCount++;
                        if (grid[x + 1, y] > 0.5f) floorCount++;
                        if (grid[x, y - 1] > 0.5f) floorCount++;
                        if (grid[x, y + 1] > 0.5f) floorCount++;

                        if (floorCount >= 3)
                        {
                            grid[x, y] = _settings.height;
                        }
                    }
                }
            }
        }
    }
}
