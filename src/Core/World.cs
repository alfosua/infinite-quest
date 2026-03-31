using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace CatalinaLabs.InfiniteQuest.Core;

public class World(WorldGenerationOptions generationOptions)
{
    public readonly Dictionary<Point, ChunkData> Chunks = [];

    public WorldGenerationOptions GenerationOptions { get; init; } = generationOptions;

    public void RevealFrom(Point worldCoords)
    {
        if (!CalculateRevealability(worldCoords.X, worldCoords.Y))
        {
            return;
        }

        var targetCell = GetCell(worldCoords);
        
        if (targetCell.IsRevealed || targetCell.IsProtected)
        {
            return;
        }

        if (targetCell is { ProximityCount: > 0 } or { IsDangerous: true })
        {
            SetCell(worldCoords, targetCell.WithStatus(CellStatus.Revealed));
            return;
        }

        var stack = new Stack<Point>();
        stack.Push(worldCoords);
        
        while (stack.Count > 0)
        {
            var coords = stack.Pop();

            if (GetCell(coords) is { IsRevealed: true })
            {
                continue;
            }
            
            int leftX = coords.X;
            while (GetCell(new Point(leftX - 1, coords.Y)) is { ProximityCount: 0, IsRevealed: false })
            {
                leftX--;
            }
            
            int rightX = coords.X;
            while (GetCell(new Point(rightX + 1, coords.Y)) is { ProximityCount: 0, IsRevealed: false })
            {
                rightX++;
            }

            for (int x = leftX - 1; x <= rightX + 1; x++)
            {
                Reveal(new Point(x, coords.Y));
            }

            ScanLineSeeds(leftX, rightX, coords.Y - 1, stack);
            ScanLineSeeds(leftX, rightX, coords.Y + 1, stack);
        }
    }

    private void ScanLineSeeds(int leftX, int rightX, int y, Stack<Point> stack)
    {
        bool added = false;
        for (int x = leftX - 1; x <= rightX + 1; x++)
        {
            var cell = GetCell(new Point(x, y));

            if (cell.ProximityCount == 0 )
            {
                if (!added)
                {
                    if (!cell.IsRevealed)
                    {
                        stack.Push(new Point(x, y));
                    }
                    added = true;
                }
            }
            else
            {
                if (cell.IsHidden)
                {
                    Reveal(new Point(x, y));
                }
                added = false;
            }
        }
    }

    public CellData GetCell(Point worldCoords)
    {
        var chunkKey = WorldCoordsToChunkKey(worldCoords);
        var chunk = GetChunk(chunkKey);
        var localCoords = WorldCoordsToChunkLocalCoords(worldCoords, chunkKey);
        return chunk.GetCell(localCoords);
    }

    public void SetCell(Point worldCoords, CellData cell)
    {
        var chunkKey = WorldCoordsToChunkKey(worldCoords);
        ref var chunk = ref GetChunk(chunkKey);
        var localCoords = WorldCoordsToChunkLocalCoords(worldCoords, chunkKey);
        chunk.SetCell(localCoords, cell);
    }

    private void Reveal(Point worldCoords)
    {
        var cell = GetCell(worldCoords);
        SetCell(worldCoords, cell.WithStatus(CellStatus.Revealed));
    }
    
    public ref ChunkData GetChunk(Point chunkKey)
    {
        // If the chunk isn't in memory, build it deterministically
        if (!Chunks.ContainsKey(chunkKey))
        {
            Chunks[chunkKey] = GenerateChunk(chunkKey);
        }

        // Return by ref to allow direct modification of the struct in the dictionary
        return ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(Chunks, chunkKey);
    }

    private ChunkData GenerateChunk(Point chunkKey)
    {
        var chunk = new ChunkData();

        int startGlobalX = chunkKey.X * ChunkData.Length;
        int startGlobalY = chunkKey.Y * ChunkData.Length;

        for (int y = 0; y < ChunkData.Length; y++)
        {
            for (int x = 0; x < ChunkData.Length; x++)
            {
                int globalX = startGlobalX + x;
                int globalY = startGlobalY + y;

                CellContent content = GetDeterministicContent(globalX, globalY);
                CellData newCell = CellData.Empty;

                if (content is not CellContent.None)
                {
                    newCell = newCell.WithContent(content);
                }
                else
                {
                    int proximity = CalculateProximity(globalX, globalY);
                    newCell = newCell.WithProximityCount(proximity);
                }
                
                chunk.SetCell(x, y, newCell);
            }
        }

        return chunk;
    }

    private CellContent GetDeterministicContent(int globalX, int globalY)
    {
        if (AreCoordsOnSafeSpot(globalX, globalY))
        {
            return CellContent.None;
        }

        uint hash = HashCoordinates(globalX, globalY, GenerationOptions.Seed);
        float normalized = (float)hash / uint.MaxValue;

        // Example Distribution Logic:
        // 85% Empty, 10% Traps, 4% Bombs, 1% Nuke
        return normalized switch
        {
            < 0.10f => CellContent.Trap,
            < 0.11f => CellContent.Bomb,
            < 0.15f => CellContent.Nuke,
            _       => CellContent.None,
        };
    }

    private int CalculateProximity(int globalX, int globalY)
    {
        int dangerCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                var content = GetDeterministicContent(globalX + dx, globalY + dy);
                
                if (content.IsDangerous)
                {
                    dangerCount++;
                }
            }
        }
        return dangerCount;
    }

    private bool CalculateRevealability(int globalX, int globalY)
    {
        if (AreCoordsOnSafeSpot(globalX, globalY))
        {
            return true;
        }

        bool foundRevealed = false;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                var cell = GetCell(new Point(globalX + dx, globalY + dy));

                if (cell.IsRevealed)
                {
                    foundRevealed = true;
                }
            }
        }
        return foundRevealed;
    }

    private bool AreCoordsOnSafeSpot(int globalX, int globalY)
    {
        var dx = Math.Abs(globalX - GenerationOptions.Origin.X);
        var dy = Math.Abs(globalY - GenerationOptions.Origin.Y);
        return dx <= 1 && dy <= GenerationOptions.SafeRadius;
    }

    private static uint HashCoordinates(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)x * 0x9E3779B1;
            h ^= (uint)y * 0x85EBCA6B;
            h = (h << 13) | (h >> 19);
            h *= 0xC2B2AE35;
            h ^= h >> 16;
            return h;
        }
    }

    private static Point WorldCoordsToChunkKey(Point coords) =>
        new Point((int)MathF.Floor((float)coords.X / ChunkData.Length), (int)MathF.Floor((float)coords.Y / ChunkData.Length));

    private static Point WorldCoordsToChunkLocalCoords(Point worldCoords, Point chunkKey) =>
        new Point(worldCoords.X - chunkKey.X * ChunkData.Length, worldCoords.Y - chunkKey.Y * ChunkData.Length);
}
