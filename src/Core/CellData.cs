using System;
using System.Diagnostics;

namespace CatalinaLabs.InfiniteQuest.Core;

public readonly struct CellData : IEquatable<CellData>
{
    private const byte StatusMask =         0b_00000_0_11;
    private const byte HasContentMask =     0b_00000_1_00;
    private const byte ProximityCountMask = 0b_01111_0_00; 
    private const byte ContentMask =        0b_11111_0_00;

    public byte Raw { get; }

    public static CellData Empty { get; } = default;

    public static CellData Void { get; } = Empty.WithStatus(CellStatus.Void);

    public CellData(byte raw) => Raw = raw;
    public CellData(int raw) => Raw = (byte)raw;

    public CellStatus Status => (CellStatus)(Raw & StatusMask);

    public bool IsHidden => Status == CellStatus.Hidden;

    public bool IsRevealed => Status == CellStatus.Revealed;

    public bool IsProtected => Status == CellStatus.Protected;

    public bool IsVoid => Status == CellStatus.Void;

    public bool HasContent => ((Raw >> 2) & 1) == 1;

    public CellContent Content => HasContent ? (CellContent)((Raw & ContentMask) >> 3) : CellContent.None;

    public bool IsDangerous => HasContent && Content is >= CellContent.Trap and <= CellContent.Nuke;

    public int ProximityCount => !HasContent ? (Raw & ProximityCountMask) >> 3 : 0;

    public CellData WithStatus(CellStatus status)
    {
        return new CellData((Raw & ~StatusMask) | MakeStatusBits(status));
    }

    public CellData WithContent(CellContent content)
    {
        Debug.Assert(content != CellContent.None, "Do not use WithContent to clear a cell. Use WithNoContent() instead.");

        return new CellData((Raw & ~ContentMask) | HasContentMask | MakeContentBits(content));
    }

    public CellData WithNoContent()
    {
        return new CellData(Raw & StatusMask);
    }

    public CellData WithProximityCount(int proximityCount)
    {
        return new CellData((Raw & StatusMask) | MakeProximityCountBits(proximityCount));
    }

    private static int MakeStatusBits(int status) => status & 0b11;
    private static int MakeStatusBits(CellStatus status) => MakeStatusBits((int)status);

    private static int MakeContentBits(int content) => (content & 0b11111) << 3;
    private static int MakeContentBits(CellContent content) => MakeContentBits((int)content);

    private static int MakeProximityCountBits(int proximityCount) => (proximityCount & 0b1111) << 3;

    public bool Equals(CellData other) => Raw == other.Raw;

    public override bool Equals(object? obj) => obj is CellData other && Equals(other);

    public override int GetHashCode() => Raw.GetHashCode();

    public static bool operator ==(CellData left, CellData right) => left.Equals(right);

    public static bool operator !=(CellData left, CellData right) => !left.Equals(right);
    
    public override string ToString() => $"Cell(Status: {Status}, Content: {Content}, Prox: {ProximityCount})";
}

public enum CellStatus : byte
{
    Hidden = 0,
    Revealed = 1,
    Protected = 2,
    Void = 3,
}

public enum CellContent : byte
{
    None = 0,
    // Dangers
    Trap = 1,
    Bomb = 2,
    Nuke = 3,
    // Collectibles
    Coin = 4,
    Chest = 5,
    Scroll = 6,
    Portal = 7,
}
