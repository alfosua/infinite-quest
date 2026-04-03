using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;

namespace CatalinaLabs.InfiniteQuest.Core;

public class MainGame : Game
{
    private const int TileSize = 32;

    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private SpriteFont font;
    private OrthographicCamera camera;
    private MouseListener mouseController;
    private World? world;
    private Point focusedCoords;
    private bool seeded = false;
    private Texture2D tileMap;
    private Color screenColor = new Color(89,70,134);

    public MainGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        graphics.SynchronizeWithVerticalRetrace = false;
        this.IsFixedTimeStep = false;
    }

    protected override void Initialize()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        camera = new OrthographicCamera(GraphicsDevice);

        mouseController = new MouseListener(new MouseListenerSettings()
        {
            DragThreshold = 16,
            DoubleClickMilliseconds = 0,
        });
        mouseController.MouseMoved += (_, e) =>
        {
            if (!IsActive)
            {
                return;
            }

            focusedCoords = CalculateCellCoordsFromScreen(e.Position, camera);
        };
        mouseController.MouseDrag += (_, e) =>
        {
            if (!IsActive)
            {
                return;
            }

            camera.Position -= e.DistanceMoved / camera.Zoom;
        };
        mouseController.MouseWheelMoved += (_, e) =>
        {
            if (!IsActive)
            {
                return;
            }

            camera.Zoom += e.ScrollWheelDelta * 0.001f;
        };
        mouseController.MouseClicked += (_, e) =>
        {
            if (!IsActive)
            {
                return;
            }

            if (!seeded && e.Button == MonoGame.Extended.Input.MouseButton.Left)
            {
                world = new World(new()
                {
                    Seed = FastRandom.Shared.Next(),
                    Origin = focusedCoords,
                    SafeRadius = 1,
                });
                seeded = true;
            }

            if (world is null)
            {
                return;
            }

            if (e.Button == MonoGame.Extended.Input.MouseButton.Left)
            {
                world.RevealFrom(focusedCoords);
            }
            else
            {
                var targetCell = world.GetCell(focusedCoords);

                if (targetCell.IsRevealed)
                {
                    return;
                }

                var nextStatus = targetCell.IsProtected
                    ? CellStatus.Hidden
                    : CellStatus.Protected;

                world.SetCell(focusedCoords, targetCell.WithStatus(nextStatus));
            }
        };

        base.Initialize();
    }

    private Point CalculateCellCoordsFromScreen(Point screenPosition, OrthographicCamera camera)
    {
        var worldPosition = camera.ScreenToWorld(screenPosition.ToVector2());
        var coordX = (int)MathF.Floor(worldPosition.X / TileSize);
        var coordY = (int)MathF.Floor(worldPosition.Y / TileSize);
        var targetCell = new Point(coordX, coordY);
        return targetCell;
    }

    protected override void LoadContent()
    {
        font = Content.Load<SpriteFont>("HudFont");
        tileMap = Content.Load<Texture2D>("fonts_k");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        mouseController.Update(gameTime);


        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(screenColor);

        var matrix = camera.GetViewMatrix();
        var fpsText = (1 / gameTime.ElapsedGameTime.TotalSeconds).ToString();

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            matrix);

        DrawWorld();

        spriteBatch.DrawRectangle(focusedCoords.X * TileSize, focusedCoords.Y * TileSize, TileSize, TileSize, Color.White, thickness: 2);
        spriteBatch.DrawString(font, fpsText, camera.Center, Color.White,
        0, Vector2.Zero, 1 / camera.Zoom, SpriteEffects.None, 1f);

        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawWorld()
    {
        if (world is null) return;

        var viewRect = camera.BoundingRectangle;
        int chunkSize = TileSize * ChunkData.Length;

        int minChunkX = (int)MathF.Floor(viewRect.Left / chunkSize);
        int maxChunkX = (int)MathF.Ceiling(viewRect.Right / chunkSize);
        int minChunkY = (int)MathF.Floor(viewRect.Top / chunkSize);
        int maxChunkY = (int)MathF.Ceiling(viewRect.Bottom / chunkSize);

        int halfTile = TileSize / 2;

        for (int cy = minChunkY; cy <= maxChunkY; cy++)
        {
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                if (!world.Chunks.TryGetValue(new Point(cx, cy), out var chunk)) continue;

                int startX = cx * chunkSize;
                int startY = cy * chunkSize;

                int currentY = startY;

                for (int i = 0; i < ChunkData.Length; i++)
                {
                    int currentX = startX;

                    for (int j = 0; j < ChunkData.Length; j++)
                    {
                        var cell = chunk.GetCell(j, i);
                        
                        var cellRect = new Rectangle(currentX, currentY, TileSize, TileSize);

                        //numeros magicos jeje :p
                        spriteBatch.Draw(tileMap, cellRect, 
                            new Rectangle((int)cell.Status * TileSize, 10, TileSize, TileSize), 
                            Color.White);

                        if (cell.IsRevealed)
                        {
                            spriteBatch.Draw(tileMap, cellRect,
                                new Rectangle((int)cell.Content * TileSize, 42, TileSize, TileSize),
                                Color.White);

                                
                            if (cell.ProximityCount > 0)
                            {   
                                var color = cell.ProximityCount switch
                                {
                                    1 => Color.Blue,
                                    2 => Color.Green,
                                    3 => Color.Red,
                                    4 => Color.Purple,
                                    5 => Color.Maroon,
                                    6 => Color.Cyan,
                                    7 => Color.Black,
                                    8 => Color.Gray,
                                    _ => Color.Black,
                                };
                                int badgeX = currentX + halfTile - 4;
                                int badgeY = currentY + halfTile - 5;

                                spriteBatch.Draw(tileMap, new Vector2(badgeX, badgeY),
                                    new Rectangle(cell.ProximityCount * 9, 0, 8, 10),
                                    color);
                            }
                        }
                        currentX += TileSize;
                    }
                    currentY += TileSize;
                }
            }
        }
    }
}
