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

class ObjectMetadata
{
    public string TypeName { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Scale { get; set; }
}

class LevelEditor : GameObject
{
    public event EventHandler<bool> OnOverlay;

    private Placeable _selectedItem;
    private List<Placeable> _placedObjects;
    private List<Placeable> _removeQueue;

    private Grid _grid;
    private Texture2D _panel;

    public bool Hidden { get; set; }
    public bool Disabled { get; set; }
    public int SnapAmount { get; set; }

    public LevelEditor()
    {
        _placedObjects = new();
        _removeQueue = new();
        _selectedItem = null;

        Hidden = true;
        SnapAmount = 5;

        ZIndex = 1;

        _panel = DebugTexture.GenerateTexture((int)GameSettings.WindowSize.X, (int)GameSettings.WindowSize.Y, Color.White);

        _grid = new(GameSettings.WindowSize, 7, 8);
        _grid.OnItemSelect += HandleItemSelect;

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

            _grid.AddItem(gameObject);
        }
    }

    public void HandleItemSelect(object sender, Placeable placeable)
    {
        var copy = placeable.Clone();
        _selectedItem = copy;
        _selectedItem.AccentColor = Color.White * 0.5f;
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
            foreach (var placeable in _placedObjects)
            {
                placeable.HandleInput();
            }

            if (_selectedItem != null)
            {
                if (Input.IsMouseJustPressed(MouseButton.Left))
                {
                    PlaceItem(_selectedItem);
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

    public void PlaceItem(Placeable placeable)
    {
        var copy = placeable.Clone();
        copy.OnDelete += HandlePlaceableDelete;
        copy.OnMove += HandlePlaceableMove;
        _placedObjects.Add(copy);
    }

    private void HandlePlaceableMove(object sender, EventArgs args)
    {
        var placeable = (Placeable)sender;

        _removeQueue.Add(placeable);
        _selectedItem = placeable;
    }

    private void HandlePlaceableDelete(object sender, EventArgs args)
    {
        _removeQueue.Add((Placeable)sender);
    }

    public override void Update(GameTime gameTime)
    {
        if (Disabled) return;

        var mouseState = Mouse.GetState();

        foreach (var placeable in _removeQueue)
        {
            _placedObjects.Remove(placeable);
        }
        _removeQueue.Clear();

        foreach (var placeable in _placedObjects)
        {
            placeable.Update(gameTime);
        }

        if (_selectedItem != null)
        {
            var position = new Vector2((mouseState.Position.X / SnapAmount) * SnapAmount, (mouseState.Position.Y / SnapAmount) * SnapAmount);
            _selectedItem.WorldPosition = position;
        }

        _grid.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, GraphicsDeviceManager graphicsDevice)
    {
        foreach (var placeable in _placedObjects.OrderBy((gameObject) => gameObject.ZIndex).ThenBy((gameObject) => gameObject.WorldPosition.Y))
        {
            placeable.Draw(spriteBatch, graphicsDevice);
        }

        if (Hidden)
        {
            _selectedItem?.Draw(spriteBatch, graphicsDevice);
        }
        else
        {
            spriteBatch.Draw(_panel, Vector2.Zero, Color.DarkGray * 0.8f);
            _grid.Draw(spriteBatch, graphicsDevice);
        }
    }

    public void SaveLevelEditor(string filename)
    {
        var metadata = new List<ObjectMetadata>();

        foreach (var placeable in _placedObjects)
        {
            var meta = new ObjectMetadata()
            {
                TypeName = placeable.Type.FullName,
                X = placeable.WorldPosition.X,
                Y = placeable.WorldPosition.Y,
                Scale = placeable.Scale,
            };

            metadata.Add(meta);
        }

        MetaManager.SaveToFile(metadata, filename);
    }

    public void LoadLevelEditor(string filename)
    {
        var metadata = MetaManager.ReadFromFile<ObjectMetadata>(filename);

        foreach (var meta in metadata)
        {
            Type type = Type.GetType(meta.TypeName);
            var position = new Vector2(meta.X, meta.Y);

            var gameObject = MetaManager.ConstructObject(type, position, meta.Scale);

            var placeable = new Placeable(gameObject, position, meta.Scale);
            placeable.OnDelete += HandlePlaceableDelete;
            placeable.OnMove += HandlePlaceableMove;

            _placedObjects.Add(placeable);
        }
    }
}