using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using TowerDefense;

class LevelEditor : GameObject
{
    public event EventHandler<bool> OnOverlay;
    public EventHandler<Placeable> OnItemPlace;

    private Placeable _selectedItem;
    private Grid _grid;
    private Texture2D _panel;

    public bool Hidden { get; set; }
    public bool Disabled { get; set; }
    public int SnapAmount { get; set; }

    public LevelEditor()
    {
        _selectedItem = null;
        _panel = DebugTexture.GenerateRectTexture((int)GameSettings.WindowSize.X, (int)GameSettings.WindowSize.Y, Color.White);
        _grid = new(GameSettings.WindowSize, 7, 8);
        _grid.OnItemSelect += HandleItemSelect;

        Hidden = true;
        SnapAmount = 5;
        ZIndex = 1;

        PopulateGrid();
    }

    public void PopulateGrid()
    {
        var saveableTypes =
            from a in AppDomain.CurrentDomain.GetAssemblies()
            from t in a.GetTypes()
            let attributes = t.GetCustomAttributes(typeof(SaveableAttribute), true)
            where attributes != null && attributes.Length > 0
            select t;

        foreach (var type in saveableTypes)
        {
            var gameObject = MetaManager.ConstructObject(type, Vector2.Zero, 1f);

            if (gameObject is TowerPlot plot)
            {
                _grid.AddItem(plot.Sprite, plot.GetType());
            }
            else if (gameObject is Decoration decor)
            {
                _grid.AddItem(decor.Sprite, decor.GetType());
            }
            else if (gameObject is PathTile path)
            {
                _grid.AddItem(path.Sprite, path.GetType());
            }
        }
    }

    public void HandleItemSelect(object sender, Placeable placeable)
    {
        var copy = placeable.Clone();
        _selectedItem = copy;
        _selectedItem.Sprite.AccentColor = Color.White * 0.5f;
    }

    public override void HandleInput()
    {
        if (Disabled) return;

        var mouseState = Mouse.GetState();

        if (Input.IsKeyJustPressed(Keys.Z))
        {
            Hidden = !Hidden;
            OnOverlay?.Invoke(this, !Hidden);
        }

        if (Input.IsKeyJustPressed(Keys.X))
        {
            _selectedItem = null;
        }

        if (Hidden)
        {
            if (_selectedItem != null)
            {
                if (Input.IsMouseJustPressed(MouseButton.Left))
                {
                    OnItemPlace?.Invoke(this, _selectedItem);
                }

                var scale = MathHelper.Clamp(mouseState.ScrollWheelValue / 1000f + 1, 0.1f, 10f);
                _selectedItem.Scale = scale;
            }
        }
        else
        {
            _grid.HandleInput();
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (Disabled) return;

        var mouseState = Mouse.GetState();

        if (_selectedItem != null)
        {
            var position = new Vector2((mouseState.Position.X / SnapAmount) * SnapAmount, (mouseState.Position.Y / SnapAmount) * SnapAmount);
            _selectedItem.WorldPosition = position;
        }

        _grid.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Hidden)
        {
            _selectedItem?.Draw(spriteBatch);
        }
        else
        {
            spriteBatch.Draw(_panel, Vector2.Zero, Color.DarkGray * 0.8f);
            _grid.Draw(spriteBatch);
        }
    }

}