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

    public MainGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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
        GraphicsDevice.Clear(Color.Gray);

        var matrix = camera.GetViewMatrix();

        spriteBatch.Begin(transformMatrix: matrix);
        
        DrawWorld();

        spriteBatch.DrawRectangle(focusedCoords.X * TileSize, focusedCoords.Y * TileSize, TileSize, TileSize, Color.White, thickness: 2);

        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawWorld()
    {
        if (world is null)
        {
            return;
        }

        var chunksToDraw = CollectionsMarshal.AsSpan(world.Chunks.ToList());
        foreach (var (key, chunk) in chunksToDraw)
        {
            var offset = new Point(key.X * TileSize * ChunkData.Length, key.Y * TileSize * ChunkData.Length);

            for (int i = 0; i < ChunkData.Length; i++)
            {
                for (int j = 0; j < ChunkData.Length; j++)
                {
                    var cell = chunk.GetCell(new Point(j, i));
                    var cellRect = new RectangleF(offset.X + j * TileSize, offset.Y + i * TileSize, TileSize, TileSize);

                    var cellColor = cell switch
                    {
                        { IsHidden: true } => Color.Gray,
                        { IsProtected: true } => Color.Yellow,
                        { Content: CellContent.Trap } => Color.Red,
                        { Content: CellContent.Bomb } => Color.DarkRed,
                        { Content: CellContent.Nuke } => Color.Green,
                        { Content: CellContent.Coin } => Color.Gold,
                        { Content: CellContent.Chest } => Color.Brown,
                        { Content: CellContent.Scroll } => Color.Beige,
                        { Content: CellContent.Portal } => Color.Purple,
                        _ => Color.Silver,
                    };

                    spriteBatch.FillRectangle(cellRect, cellColor);
                    spriteBatch.DrawRectangle(cellRect, Color.Black);

                    if (cell.IsRevealed && cell.ProximityCount > 0)
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
                        var badge = cell.ProximityCount.ToString();
                        var textSize = font.MeasureString(badge);
                        var badgeX = cellRect.X + cellRect.Width / 2 - textSize.X / 2;
                        var badgeY = cellRect.Y + cellRect.Height / 2 - textSize.Y / 2;
                        spriteBatch.DrawString(font, badge, new Vector2(badgeX, badgeY), color);
                    }
                }
            }
        }
    }

}
