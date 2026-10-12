using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Graphics;

namespace PhisicsGame.Gameplay;

public sealed class TileMap
{
    public const int TileSize = 48;
    private readonly int[,] _tiles;

    public TileMap()
    {
        var cols = VirtualResolution.Width / TileSize;
        var rows = VirtualResolution.Height / TileSize;
        _tiles = new int[rows, cols];

        FillBorderOnly();
    }

    private void FillBorderOnly()
    {
        for (var y = 0; y < _tiles.GetLength(0); y++)
        {
            for (var x = 0; x < _tiles.GetLength(1); x++)
            {
                var isBorder = x == 0 || y == 0 || x == _tiles.GetLength(1) - 1 || y == _tiles.GetLength(0) - 1;
                _tiles[y, x] = isBorder ? 1 : 0;
            }
        }
    }

    public Rectangle GetBounds() =>
        new(0, 0, _tiles.GetLength(1) * TileSize, _tiles.GetLength(0) * TileSize);

    public bool IsWalkable(Vector2 position)
    {
        var tileX = (int)(position.X / TileSize);
        var tileY = (int)(position.Y / TileSize);

        if (tileX < 0 || tileY < 0 || tileX >= _tiles.GetLength(1) || tileY >= _tiles.GetLength(0))
        {
            return false;
        }

        return _tiles[tileY, tileX] == 0;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        for (var y = 0; y < _tiles.GetLength(0); y++)
        {
            for (var x = 0; x < _tiles.GetLength(1); x++)
            {
                var rect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                Color color;
                if (_tiles[y, x] == 1)
                {
                    color = new Color(20, 26, 42);
                }
                else
                {
                    color = new Color(26, 40, 58);
                }

                spriteBatch.Draw(pixel, rect, color);
                spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), new Color(76, 201, 240) * 0.08f);
                spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), new Color(76, 201, 240) * 0.08f);
            }
        }
    }
}
