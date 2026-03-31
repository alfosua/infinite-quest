using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace CatalinaLabs.InfiniteQuest.Core;

public struct ChunkData
{
    public const int Length = 8;

    [InlineArray(Length*Length)]
    public struct CellArray
    {
        private byte _e0;
    }

    private CellArray Cells;

    private static int CalcCoordsAsIndex(int x, int y)
    {
        Debug.Assert(x >= 0 && x < Length && y >= 0 && y < Length, "coords out of bounds");

        return y * Length + x;
    }

    public readonly CellData GetCell(int x, int y) => new(Cells[CalcCoordsAsIndex(x, y)]);

    public readonly CellData GetCell(Point p) => GetCell(p.X, p.Y);

    public void SetCell(int x, int y, CellData cell)
    {
        Cells[CalcCoordsAsIndex(x, y)] = cell.Raw;
    }

    public void SetCell(Point p, CellData cell) => SetCell(p.X, p.Y, cell);

    public void Reveal(int x, int y)
    {
        var cell = GetCell(x, y);
        SetCell(x, y, cell.WithStatus(CellStatus.Revealed));
    }

    public void Reveal(Point p) => Reveal(p.X, p.Y);
}
