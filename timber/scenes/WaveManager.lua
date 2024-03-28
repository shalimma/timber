local Godot = require("Godot")
local Timer = require("Timer")
local WaveManager = Godot.Class("WaveManager", Godot.Node)

-- variables and stuff
function WaveManager._init(self)
    -- lists
    self.wave_spawn_positions = {}
    self.enemies = {}

    self.spawnTimer = Godot.Timer.new()
    self.waveTimer = Godot.Timer.new()

    self.waveBar = nil

    -- set init wave data
    self.currentWavePosition = 0
    self.enemiesToSpawn = 0
    self.enemyConfig = nil
    self.waves = {
        {waveTiming = 10, enemyCount = 10},
        -- convert to better format
    }
end

function WaveManager._ready(self)
    self.spawnTimer:set_wait_time(1)
    self.spawnTimer:set_one_shot(true)
    self:add_child(self.spawnTimer)
    self.spawnTimer:connect("timeout", self, "_on_spawn_enemy")

    self.waveTimer:set_wait_time(1)
    self.waveTimer:set_one_shot(false)
    self:add_child(self.waveTimer)
    self.waveTimer:connect("timeout", self, "_on_wave_tick")

    self.waveBar = self:get_node("WaveBar")
    self.currentWavePosition = 0
    self.enemiesToSpawn = 0
end

function WaveManager._process(self, delta)
    if Godot.Input.is_action_just_pressed("middle_click") then
        -- Implementation of middle click action
    end
end

function WaveManager._get_ray_plane_intersection(self, ray, plane_normal)
    local t = -plane_normal:dot(ray.start) / plane_normal:dot(ray.direction)
    return ray.start + t * ray.direction
end

function WaveManager._on_spawn_enemy(self)
    print("A new challenger has appeared!")
    

    if self.enemyConfig == nil then return end

    print(self.enemyConfig.name)
    for _, position in ipairs(self.wave_spawn_positions) do
        self:_spawn_wave_enemy(self.enemyConfig, position)
    end

    self.enemiesToSpawn = self.enemiesToSpawn - 1

    if self.enemiesToSpawn <= 0 then
        self.spawnTimer:stop()
    end
end

function WaveManager._spawn_wave_enemy(self, config, position)
    local actorScene = Godot.ResourceLoader:load("res://scenes/actor.tscn")
    local newActor = actorScene:instance()
    newActor:set_name(config.name)
    self:add_child(newActor)

    local actorScript = newActor
    newActor:set_global_transform(position)

    actorScript:configure(config)

    for _, scriptName in ipairs(config.scripts) do
        local sourcePath = Godot.OS:get_current_dir() .. "/resources/scripts/" .. scriptName .. ".gd"
        self:_load_script_at_location(sourcePath, newActor)
    end

    table.insert(self.enemies, actorScript)

    local sm = actorScript:find_node("StateManager")
    if sm and sm.states["MovementState"] then
        local b = sm.states["MovementState"]

    end
end

-- function WaveManager._load_script_at_location(self, location, owning_actor)
--     -- Implementation of loading script
-- end

-- Function to set enemy configuration
-- function WaveManager.set_enemy_config(self, e)
--     print("Loaded " .. e.enemyConfig.name .. " as wave enemy!")
--     self.enemyConfig = e.enemyConfig
-- end

-- called on wave tick
function WaveManager._on_wave_tick(self)
    self.currentWavePosition = self.currentWavePosition + self.waveTimer:get_wait_time()
    self.waveBar:set_value(self.currentWavePosition)
    print(self.currentWavePosition)

    for _, wave in ipairs(self.waves) do
        if wave.waveTiming == self.currentWavePosition then
            self.spawnTimer:start()
            self.enemiesToSpawn = wave.enemyCount
            print("A new wave is starting!")
        end
    end
end

return WaveManager
