using Godot;
using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;

public class CombatState : ActorState
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    int attackRange = 2;//number of grids

    float attackWindup = 0.5f;//animation before attack
    float attackRecovery = 0.125f;//anim after attack
    float attackCooldown = 0.5f;

    bool attackable = true;
    bool attacking = false;
    bool moving = false;

    public Actor TargetActor;

    string team;

    float time = 0.0f, rotateTime = 0.0f;

    public override string name
    {
        get { return "CombatState"; }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
    }

    public override void Start()
    {
        inclusiveStates = new HashSet<string>();
        ArborCoroutine.StopCoroutinesOnNode(this);
        animation_offset = GD.Randf() * 100.0f;
        team = actor.GetNode<HasTeam>("HasTeam").team;
        attackable = true;
        GD.Print(attackable);

    }

    public override void Update(float delta)
    {
        if (TargetActor != null)//TODO check if actor is dead
        {
            Vector3 dest = Grid.LockToGrid(TargetActor.GlobalTranslation);
            MovementState b = (manager.states["MovementState"] as MovementState);

            float dist = (Grid.ConvertToCoord(actor.GlobalTranslation) - Grid.ConvertToCoord(dest)).Mag();

            if (dist > attackRange && attackable)
            {
                if (b.waypoints.Count == 0)
                {
                    ArborCoroutine.StopCoroutinesOnNode(b);
                    ArborCoroutine.StartCoroutine(TestMovement.PathFindAsync(actor.GlobalTranslation, dest, (List<Vector3> a) =>
                    {
                        if (a.Count > 0)
                        {
                            manager.EnableState("MovementState");
                            b.waypoints = a;
                        }
                    }), b);
                    manager.DisableState("CombatState");
                    return;
                }
            }
            else if (attackable)
            {
                ArborCoroutine.StartCoroutine(attackAnimation(), this);
            }
        }
        else
        {
            manager.DisableState("CombatState");
            return;
        }
    }

    IEnumerator attackAnimation()
    {
        attacking = true;
        attackable = false;
        GD.Print("attack");
        yield return ArborCoroutine.WaitForSeconds(attackWindup);
        TargetActor.Hurt();
        attacking = false;

        yield return ArborCoroutine.WaitForSeconds(attackRecovery);
        rotateTime = 0.0f;
        yield return ArborCoroutine.WaitForSeconds(attackCooldown);
        GD.Print("end attack");
        attackable = true;
        
    }

    float animation_offset = 0;
    public override void Animate(float delta)
    {
        time += delta;
        /* Return unit to cell if it has been moved by other states */
        actor.view.Translation += (Vector3.Zero - actor.view.Translation) * 0.1f;

        /* idle / breathing animation */
        float idle_scale_impact = (1.0f + Mathf.Sin(time * 4 + animation_offset) * 0.025f);

        /* Paper Turning */
        float current_scale_x = actor.view.Scale.x;
        current_scale_x += (actor.GetDesiredScaleX() - current_scale_x) * 0.2f;
        actor.view.Scale = new Vector3(current_scale_x, actor.initial_view_scale.y * idle_scale_impact, actor.view.Scale.z);

        if (attacking)
        {
            rotateTime += delta;
            const float rot_amplitude = 5f;
            int direction = -1;
            if (TargetActor.GlobalTranslation.x < actor.GlobalTranslation.x) direction = 1;

            actor.view.Rotation = actor.initial_rotation + new Vector3(0, 0, direction * rot_amplitude * rotateTime * (rotateTime - 0.5f));
        }
        
    }
}