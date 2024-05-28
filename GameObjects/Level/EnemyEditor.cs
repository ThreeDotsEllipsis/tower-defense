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

class EnemyEditor : GameObject
{
    private GridEnemy _grid;
    private Texture2D _panel;
    private WalkPath _walkPath;
    private WaveManager _waveManager;

    private int _prevWave = 0;
    public InputForm WaveInput { get; }

    public int NodeId { get; private set; }
    public bool Hidden { get; private set; }

    public EnemyEditor(GameObject parent, WalkPath walkPath) : base(parent)
    {
        WaveInput = new InputForm(this, "Wave", Vector2.Zero, 1f);
        WaveInput.LocalPosition += new Vector2(
            GameSettings.WindowWidth - WaveInput.Sprite.Size.X / 2f,
            WaveInput.Sprite.Size.Y / 2f
        );

        _panel = DebugTexture.GenerateRectTexture((int)GameSettings.WindowSize.X, (int)GameSettings.WindowSize.Y, Color.White);
        _grid = new GridEnemy(this, GameSettings.WindowSize, 7, 8);
        _grid.LocalPosition += new Vector2(0, WaveInput.Sprite.Size.Y);
        _walkPath = walkPath;
        _waveManager = new WaveManager(this, _walkPath, true);

        NodeId = 0;
        Hidden = true;

        PopulateGrid();

        WaveInput.NumberInput.OnValueChange += HandleWaveChange;
    }

    private void HandleWaveChange(object sender, int wave)
    {
        StoreToManager();
        LoadFromManager();

        _prevWave = wave;
    }

    public void PopulateGrid()
    {
        var basicOrk = new BasicOrk(null, _walkPath, null, 1f);

        _grid.AddItem(basicOrk.Sprite, basicOrk.GetType());
    }

    public void Show(int nodeId)
    {
        WaveInput.NumberInput.Reset();
        _prevWave = 0;

        Hidden = false;
        NodeId = nodeId;

        LoadFromManager();
    }

    private void LoadFromManager()
    {
        foreach (var item in _grid.Items)
        {
            var info = _waveManager.GetEnemyInfo(NodeId, WaveInput.NumberInput.Value, item.Type.FullName);

            item.OrderInput.NumberInput.Value = info.Order;
            item.AmountInput.NumberInput.Value = info.Amount;
        }
    }

    private void StoreToManager()
    {
        foreach (var item in _grid.Items)
        {
            _waveManager.StoreEnemyInfo(
                NodeId, _prevWave, item.Type.FullName,
                item.OrderInput.NumberInput.Value,
                item.AmountInput.NumberInput.Value
            );
        }
    }

    public override void HandleInput()
    {
        if (EditLevelState.EditState != EditState.EnemyEditor) return;

        if (Input.IsKeyJustPressed(Keys.Z))
        {
            HandleWaveChange(null, 0);
            Hidden = true;
        }

        if (Input.IsKeyJustPressed(Keys.Q))
        {
            _waveManager.SaveToFile("enemy_editor");
        }
        else if (Input.IsKeyJustPressed(Keys.R))
        {
            _waveManager.LoadFromFile("enemy_editor");
        }

        base.HandleInput();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Hidden) return;

        spriteBatch.Draw(_panel, Vector2.Zero, Color.DarkGray * 0.8f);

        base.Draw(spriteBatch);
    }
}