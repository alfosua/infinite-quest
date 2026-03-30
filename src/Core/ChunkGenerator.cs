using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace CatalinaLabs.InfiniteQuest.Core;

public static class ChunkGenerator
{
    public static ChunkData Generate(ChunkGenerationInput input)
    {
        var chunk = new ChunkData(input.Length);

        int dangerLeft = input.DangerCount;
        while (dangerLeft > 0)
        {
            var x = FastRandom.Shared.Next() % chunk.Length;
            var y = FastRandom.Shared.Next() % chunk.Length;
            var targetCoords = new Point(x, y);

            if (targetCoords == input.SafeSpot)
            {
                continue;
            }

            var targetCell = chunk.GetCell(targetCoords);

            if (targetCell.HasContent)
            {
                continue;
            }

            var dangerWeight = FastRandom.Shared.NextSingle();
            var cellContent = dangerWeight switch
            {
                > 0.1f => CellContent.Trap,
                > 0.01f => CellContent.Bomb,
                _ => CellContent.Nuke,
            };

            chunk.SetCell(targetCoords, targetCell.WithContent(cellContent));
            dangerLeft--;

            void IncrementDanger(int x, int y)
            {
                var cellToIncrement = chunk.GetCell(x, y);
                chunk.SetCell(x, y, cellToIncrement.WithProximityCount(cellToIncrement.ProximityCount + 1));
            }

            for (int sx = targetCoords.X - 1; sx <= targetCoords.X + 1; sx++)
            {
                for (int sy = targetCoords.Y - 1; sy <= targetCoords.Y + 1; sy++)
                {
                    if (sx < 0 || sx >= chunk.Length || sy < 0 || sy >= chunk.Length)
                    {
                        continue;
                    }

                    if (sx == targetCoords.X && sy == targetCoords.Y)
                    {
                        continue;
                    }

                    if (chunk.GetCell(sx, sy).HasContent)
                    {
                        continue;
                    }

                    IncrementDanger(sx, sy);
                }
            }
        }

        return chunk;
    }
}

public record ChunkGenerationInput
{
    public int Length { get; init; }

    public Point SafeSpot { get; init; }

    public int SafeSpotRadius { get; init; }

    public int DangerCount { get; init; }
}
