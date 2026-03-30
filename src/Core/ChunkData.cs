using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace CatalinaLabs.InfiniteQuest.Core;

public readonly struct ChunkData
{
    public readonly int Length;

    private readonly CellData[] Cells;

    public ChunkData(int length)
    {
        Length = length;
        Cells = ArrayPool<CellData>.Shared.Rent(length*length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCellIndexByCoords(int x, int y) => y * Length + x;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref CellData GetCellRef(int x, int y)
    {
        return ref Cells[GetCellIndexByCoords(x, y)];
    }

    public CellData GetCell(int x, int y) => Cells[GetCellIndexByCoords(x, y)];

    public CellData GetCell(Point p) => GetCell(p.X, p.Y);

    public void SetCell(int x, int y, CellData cell)
    {
        ref CellData target = ref GetCellRef(x, y);
        target = cell;
    }

    public void SetCell(Point p, CellData cell) => SetCell(p.X, p.Y, cell);

    public void Reveal(int x, int y)
    {
        ref CellData target = ref GetCellRef(x, y);
        target = target.WithStatus(CellStatus.Revealed);
    }

    public void Reveal(Point p) => Reveal(p.X, p.Y);
}
