using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TowerDefense;

public class Line : GameObject
{
    private float _lineRotation;
    private Vector2 _lineOrigin;
    private Texture2D _texture;

    public int Thickness { get; set; }
    public Vector2 StartPosition { get; set; }
    public Vector2 EndPosition { get; set; }
    public Color LineColor { get; set; }

    public Line(GameObject parent, Vector2 startPosition, Vector2 endPosition, Color color, int thickness) : base(parent)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        Thickness = thickness;
        LineColor = color;

        Recalculate();
    }

    public void Recalculate()
    {
        var distance = (int)Vector2.Distance(StartPosition, EndPosition);
        _texture = new Texture2D(DebugTexture.graphicsDevice.GraphicsDevice, distance, Thickness);

        var data = new Color[distance * Thickness];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = LineColor;
        }
        _texture.SetData(data);

        _lineRotation = (float)Math.Atan2(EndPosition.Y - StartPosition.Y, EndPosition.X - StartPosition.X);
        _lineOrigin = new Vector2(0, Thickness / 2);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        spriteBatch.Draw(_texture, StartPosition, null, Color.White, _lineRotation, _lineOrigin, 1.0f, SpriteEffects.None, 1.0f);
    }
}