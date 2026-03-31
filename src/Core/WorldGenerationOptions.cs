using System;
using Microsoft.Xna.Framework;

namespace CatalinaLabs.InfiniteQuest.Core;

public record WorldGenerationOptions
{
    public int Seed { get; init; } = (int)DateTime.Now.Ticks;

    public Point Origin { get; init; }

    public int SafeRadius { get; init; }
}
