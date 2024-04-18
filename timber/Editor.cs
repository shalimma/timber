using Godot;
using System;

public class Editor : Control
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.

    public enum BuildingType
    {
        None,
        Tower,
        Turret,
        Flag,
        Lighthouse
        // Add more types as needed
    }

    private BuildingType currentBuildingType = BuildingType.None;

    public override void _Ready()
    {
        // GetNode<Button>("TowerButton").Connect("pressed", this, nameof(OnTowerButtonPressed));
        // GetNode<Button>("TurretButton").Connect("pressed", this, nameof(OnTurretButtonPressed));
        // GetNode<Button>("FlagButton").Connect("pressed", this, nameof(OnFlagButtonPressed));
        // GetNode<Button>("LighthouseButton").Connect("pressed", this, nameof(OnLighthouseButtonPressed));

        EventBus.Subscribe<TowerManager.EventToggleTowerPlacement>(OnEventToggleTowerPlacement);
        EventBus.Subscribe<TowerManager.EventCancelTowerPlacement>(OnEventCancelTowerPlacement);
    }

    public void OnTurretButtonPressed()
    {
        currentBuildingType = BuildingType.Turret;
        GD.Print("Current building type set to Turret");
    }

    public void OnTowerButtonPressed()
    {
        currentBuildingType = BuildingType.Tower;
        GD.Print("Current building type set to Tower");
    }

    public void OnFlagButtonPressed()
    {
        currentBuildingType = BuildingType.Flag;
        GD.Print("Current building type set to Flag");
    }

    public void OnLighthouseButtonPressed()
    {
        currentBuildingType = BuildingType.Lighthouse;
        GD.Print("Current building type set to Lighthouse");
    }

    void OnEventToggleTowerPlacement(TowerManager.EventToggleTowerPlacement e)
    {
        var node = GetNode<CanvasLayer>("EditorCanvas");
        if (node != null)
        {
            if (node.Visible)
                node.Hide();
            else
                node.Show();
        }
    }

    void OnEventCancelTowerPlacement(TowerManager.EventCancelTowerPlacement e)
    {
        var node = GetNode<CanvasLayer>("EditorCanvas");
        node.Hide();


    }

        /*
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey eventKey)
            {
                if (eventKey.Pressed && eventKey.Scancode == (uint)KeyList.T)
                {

                    var node = GetNode<CanvasLayer>("EditorCanvas");
                    if (node != null)
                    {
                        if (node.Visible)
                            node.Hide();
                        else
                            node.Show();
                    }
                }
            }

        }
        */

        //  // Called every frame. 'delta' is the elapsed time since the previous frame.
        //  public override void _Process(float delta)
        //  {
        //      
        //  }
    }
