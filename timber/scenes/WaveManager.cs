using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using Priority_Queue;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Diagnostics;
using static Godot.Control;

public class WaveManager : Node
{
    

    List<Vector3> wave_spawn_positions = new List<Vector3>();
    List<Actor> enemies = new List<Actor>();

    [Export] private Timer spawnTimer;
    [Export] private Timer waveTimer;
    [Export] private ProgressBar waveBar;

    private float currentWavePosition;
    private int enemiesToSpawn;

    public ActorConfig enemyConfig;

    public class WaveData
    {
        public float waveTiming;
        public int enemyCount;

        public WaveData(float waveTiming, int enemyCount)
        {
            this.waveTiming = waveTiming;
            this.enemyCount = enemyCount;
        }
    }

    [Export] List<WaveData> waves = new List<WaveData>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        spawnTimer = GetNode<Timer>("SpawnTimer");
        spawnTimer.Connect("timeout", this, nameof(OnSpawnEnemy));

        waveTimer = GetNode<Timer>("WaveTimer");
        waveTimer.Connect("timeout", this, nameof(OnWaveTick));

        waveBar = GetNode<ProgressBar>("WaveBar");

        EventBus.Subscribe<EnemyDataLoadedEvent>(SetEnemyConfig);

        currentWavePosition = 0;
        enemiesToSpawn = 0;

        waves.Add(new WaveData(10, 10));
        waves.Add(new WaveData(30, 3));
        waves.Add(new WaveData(50, 10));
        waves.Add(new WaveData(70, 20));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (Input.IsActionJustPressed("middle_click"))
        {
            //From SelectionSystem.cs
            var from = GameplayCamera.GetGameplayCamera().ProjectRayOrigin(SelectionSystem.GetCursorWindowPosition());
            var dir = GameplayCamera.GetGameplayCamera().ProjectRayNormal(SelectionSystem.GetCursorWindowPosition());

            Vector3 intersection_point = GetRayPlaneIntersection(new Ray(from, dir), Vector3.Up);
            //DebugSphere.VisualizePoint(intersection_point);

            /* Round */
            Vector3 rounded_point = Grid.LockToGrid(intersection_point);

            wave_spawn_positions.Add(rounded_point);

            GD.Print("Wave Spawn Positions:");
            foreach (var position in wave_spawn_positions)
            {
                GD.Print(position);
            }
        }
        
    }

    Vector3 GetRayPlaneIntersection(Ray ray, Vector3 plane_normal)
    {
        float t = -(plane_normal.Dot(ray.start)) / (plane_normal.Dot(ray.direction));
        return ray.start + t * ray.direction;
    }

    private void OnSpawnEnemy()
    {
        GD.Print("a new challenger has appeared!");

        if (enemyConfig == null) return;

        GD.Print(enemyConfig.name);
        foreach (var position in wave_spawn_positions)
        {
            SpawnWaveEnemy(enemyConfig, position);
        }

        enemiesToSpawn--;

        if (enemiesToSpawn <= 0)
            spawnTimer.Stop();
    }

    void SpawnWaveEnemy(ActorConfig config, Vector3 position)
    {
        /* Spawn actor scene */
        PackedScene actor_scene = (PackedScene)ResourceLoader.Load("res://scenes/actor.tscn");
        Spatial new_actor = (Spatial)actor_scene.Instance();
        new_actor.Name = config.name;
        AddChild(new_actor);

        Actor actor_script = new_actor as Actor;

        new_actor.GlobalTranslation = position;

        actor_script.Configure(config);

        /* customize actor aesthetics */


        /* Load scripts of an actor */
        foreach (string script_name in config.scripts)
        {
            string source_path = System.IO.Directory.GetCurrentDirectory() + @"\resources\scripts\" + script_name + ".gd";
            LoadScriptAtLocation(source_path, new_actor);
        }

        enemies.Add(actor_script);

        GD.Print(actor_script.Name);
        StateManager sm = actor_script.FindNode("StateManager") as StateManager;
        if (!sm.states.ContainsKey("MovementState")) return;

        MovementState b = (sm.states["MovementState"] as MovementState);

        ArborCoroutine.StopCoroutinesOnNode(b);
        ArborCoroutine.StartCoroutine(TestMovement.PathFindAsync(actor_script.GlobalTranslation, new Vector3(0, 0, 0), (List<Vector3> a) => {
            if (a.Count > 0)
            {
                sm.EnableState("MovementState");
                b.waypoints = a;
            }
        }), b);
    }

    void LoadScriptAtLocation(string location, Node owning_actor)
    {
        return;
        GD.Print("Attempting to load external file [" + location + "]");
        Script loaded_gdscript = (Script)GD.Load(location);

        Node new_script_node = new Node();
        new_script_node.SetScript(loaded_gdscript);

        //new_script_node.Call("_Ready");
        owning_actor.AddChild(new_script_node);
        new_script_node._Ready();
        new_script_node.SetProcess(true);
        GD.Print("Done attaching external script [" + location + "] to actor [" + owning_actor.Name + "]");
    }

    public void SetEnemyConfig(EnemyDataLoadedEvent e)
    {
        GD.Print("Loaded " + e.enemyConfig.name + " as wave enemy!");
        enemyConfig = e.enemyConfig;
    }

    private void OnWaveTick()
    {
        currentWavePosition += waveTimer.WaitTime;
        waveBar.Value = currentWavePosition;
        GD.Print(currentWavePosition);

        
        foreach (WaveData wave in waves)
        {
            if(wave.waveTiming == currentWavePosition)
            {
                spawnTimer.Start();
                enemiesToSpawn = wave.enemyCount;
                GD.Print ("A new wave is starting!");
            }
        }
        
    }
}