using System;
using System.Collections.Generic;
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
    private ChunkData chunk = new ChunkData(16);
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
            focusedCoords = CalculateCellCoordsFromScreen(e.Position, camera);
        };
        mouseController.MouseDrag += (_, e) =>
        {
            camera.Position -= e.DistanceMoved / camera.Zoom;
        };
        mouseController.MouseWheelMoved += (_, e) =>
        {
           camera.Zoom += e.ScrollWheelDelta * 0.001f; 
        };
        mouseController.MouseClicked += (_, e) =>
        {
            if (!seeded && e.Button == MonoGame.Extended.Input.MouseButton.Left)
            {
                chunk = ChunkGenerator.Generate(new()
                {
                    Length = 16,
                    SafeSpot = focusedCoords,
                    SafeSpotRadius = 1,
                    DangerCount = 40,
                });
                seeded = true;
            }

            if (e.Button == MonoGame.Extended.Input.MouseButton.Left)
            {
                RevealFrom(focusedCoords, chunk);
            }
            else
            {
                var targetCell = chunk.GetCell(focusedCoords);

                if (targetCell.IsRevealed)
                {
                    return;
                }

                var nextStatus = targetCell.IsProtected
                    ? CellStatus.Hidden
                    : CellStatus.Protected;

                chunk.SetCell(focusedCoords, targetCell.WithStatus(nextStatus));
            }
        };

        base.Initialize();
    }

    private void RevealFrom(Point targetCoords, ChunkData chunk)
    {
        var targetCell = chunk.GetCell(targetCoords);
        
        if (targetCell.IsRevealed || targetCell.IsProtected)
        {
            return;
        }

        if (targetCell is { ProximityCount: > 0 } or { IsDangerous: true })
        {
            chunk.SetCell(targetCoords, targetCell.WithStatus(CellStatus.Revealed));
            return;
        }

        var stack = new Stack<Point>();
        stack.Push(targetCoords);
        
        while (stack.Count > 0)
        {
            var coords = stack.Pop();

            if (chunk.GetCell(coords) is { IsRevealed: true })
            {
                continue;
            }
            
            int leftX = coords.X;
            while (leftX > 0 && chunk.GetCell(leftX - 1, coords.Y) is { ProximityCount: 0, IsRevealed: false })
            {
                leftX--;
            }
            
            int rightX = coords.X;
            while (rightX < chunk.Length - 1 && chunk.GetCell(rightX + 1, coords.Y) is { ProximityCount: 0, IsRevealed: false })
            {
                rightX++;
            }

            for (int x = Math.Max(0, leftX - 1); x <= Math.Min(chunk.Length - 1, rightX + 1); x++)
            {
                chunk.Reveal(x, coords.Y);
            }

            ScanLineSeeds(leftX, rightX, coords.Y - 1, stack, chunk);
            ScanLineSeeds(leftX, rightX, coords.Y + 1, stack, chunk);
        }
    }

    private void ScanLineSeeds(int leftX, int rightX, int y, Stack<Point> stack, ChunkData chunk)
    {
        if (y < 0 || y >= chunk.Length)
        {
            return;
        }

        bool added = false;
        for (int x = Math.Max(0, leftX - 1); x <= Math.Min(chunk.Length - 1, rightX + 1); x++)
        {
            var cell = chunk.GetCell(x, y);

            if (cell.ProximityCount == 0 )
            {
                if (!added)
                {
                    stack.Push(new Point(x, y));
                    added = true;
                }
            }
            else
            {
                if (cell.IsHidden)
                {
                    chunk.Reveal(x, y);
                }
                added = false;
            }
        }
    }

    private Point CalculateCellCoordsFromScreen(Point screenPosition, OrthographicCamera camera)
    {
        var worldPosition = camera.ScreenToWorld(screenPosition.ToVector2());
        var coordX = (int)Math.Clamp(MathF.Floor(worldPosition.X / TileSize), 0, chunk.Length - 1);
        var coordY = (int)Math.Clamp(MathF.Floor(worldPosition.Y / TileSize), 0, chunk.Length - 1);
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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var matrix = camera.GetViewMatrix();
        
        spriteBatch.Begin(transformMatrix: matrix);

        for (int i = 0; i < chunk.Length; i++)
        {
            for (int j = 0; j < chunk.Length; j++)
            {
                var cell = chunk.GetCell(j, i);
                var cellRect = new RectangleF(j * TileSize, i * TileSize, TileSize, TileSize);
                
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
                    spriteBatch.DrawString(font, cell.ProximityCount.ToString(), new Vector2(cellRect.X, cellRect.Y), Color.Black);
                }
            }
        }

        spriteBatch.DrawRectangle(focusedCoords.X * TileSize, focusedCoords.Y * TileSize, TileSize, TileSize, Color.White, thickness: 2);

        spriteBatch.End();

        base.Draw(gameTime);
    }
}
