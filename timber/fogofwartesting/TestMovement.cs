using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using Priority_Queue;
using Newtonsoft.Json.Serialization;

public class TestMovement : Node
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    bool isPressed = false;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (!isPressed && Input.IsKeyPressed((int)KeyList.Y))
        {
            //From SelectionSystem.cs
            var from = GameplayCamera.GetGameplayCamera().ProjectRayOrigin(SelectionSystem.GetCursorWindowPosition());
            var dir = GameplayCamera.GetGameplayCamera().ProjectRayNormal(SelectionSystem.GetCursorWindowPosition());

            Vector3 intersection_point = GetRayPlaneIntersection(new Ray(from, dir), Vector3.Up);
            //DebugSphere.VisualizePoint(intersection_point);

            /* Round */
            Vector3 rounded_point = new Vector3(Mathf.RoundToInt(intersection_point.x / 2.0f) * 2.0f, 0.1f, Mathf.RoundToInt(intersection_point.z / 2.0f) * 2.0f);

            //Move to position

            foreach (var entity in SelectionSystem.GetCurrentActiveSelectables())
            {
                //Maybe better way to do this w/o reflection?
                if (typeof(Actor).IsAssignableFrom(entity.GetParent().GetType()))
                {
                    var actor = (Actor)entity.GetParent();
                    StateManager sm = actor.FindNode("StateManager") as StateManager;
                    string team = actor.GetNode<HasTeam>("HasTeam").team;
                    if (!sm.states.ContainsKey("MovementState")) continue;
                    if (team != "player") continue;

                    sm.EnableState("MovementState");
                    List<Vector3> points = PathFind(actor.GlobalTranslation, rounded_point);
                    (sm.states["MovementState"] as MovementState).waypoints = points; //override previous waypoints

                }
            }
        }
        //isPressed = Input.IsKeyPressed((int)KeyList.Y);
    }
    public static List<Vector3> PathFind(Vector3 cur, Vector3 dest)
    {
        //Theta* https://en.wikipedia.org/wiki/Theta*
        //Priority Queue https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
        int curX = Mathf.RoundToInt(cur.x / 2.0f);
        int curZ = Mathf.RoundToInt(cur.z / 2.0f);
        int destX = Mathf.RoundToInt(dest.x / 2.0f);
        int destZ = Mathf.RoundToInt(dest.z / 2.0f);

        Coord destCoord = new Coord(destX, destZ);

        //LuaLoader.tileData;

        FastPriorityQueue<ThetaNode> open = new FastPriorityQueue<ThetaNode>(10000);
        ThetaNode origin = new ThetaNode(new Coord(curX, curZ), null);
        origin.gScore = 0f;
        open.Enqueue(origin, 0f);
        HashSet<ThetaNode> closed = new HashSet<ThetaNode>();
        Dictionary<Coord, ThetaNode> createdNodes = new Dictionary<Coord, ThetaNode>();
        createdNodes[new Coord(curX, curZ)] = origin;


        List<Coord> directions = new List<Coord>
        {
            new Coord(-1, 0),
            new Coord(1, 0),
            new Coord(0, -1),
            new Coord(0, 1)
        };

        while (open.Count > 0)
        {
            ThetaNode curNode = open.Dequeue();
            if (curNode.x == destX && curNode.z == destZ)
            {
                List<Vector3> ans = new List<Vector3>();
                while (curNode.parent != null)
                {
                    ans.Add(new Vector3(curNode.x * 2f, cur.y, curNode.z * 2f));
                    curNode = curNode.parent;
                }
                ans.Reverse();
                return ans;
            }
            closed.Add(curNode);

            foreach (var dir in directions)
            {
                Coord neighbor = curNode.coord + dir;
                if (neighbor.x < 0 || neighbor.z < 0 || neighbor.x > Grid.width - 1 || neighbor.z > Grid.height - 1) continue;
                if (Grid.tiledata[neighbor.z][neighbor.x].value != '.') continue;

                if (!createdNodes.ContainsKey(neighbor))
                {
                    createdNodes[neighbor] = new ThetaNode(neighbor, curNode);
                }

                //Update Node
                ThetaNode neighborNode = createdNodes[neighbor];

                if (closed.Contains(neighborNode)) continue;

                if (curNode.parent != null
                    && curNode.parent.gScore + (curNode.parent.coord - neighbor).Mag() < neighborNode.gScore
                    && LineOfSight(curNode.parent.coord, neighbor))
                {
                    neighborNode.gScore = curNode.parent.gScore + (curNode.parent.coord - neighbor).Mag();
                    neighborNode.parent = curNode.parent;
                    if (open.Contains(neighborNode))
                    {
                        open.Remove(neighborNode);
                    }
                    open.Enqueue(neighborNode, neighborNode.gScore + (destCoord - neighbor).Mag());
                }
                else if (curNode.gScore + (curNode.coord - neighbor).Mag() < neighborNode.gScore)
                {
                    neighborNode.gScore = curNode.gScore + (curNode.coord - neighbor).Mag();
                    neighborNode.parent = curNode;
                    if (open.Contains(neighborNode))
                    {
                        open.Remove(neighborNode);
                    }
                    open.Enqueue(neighborNode, neighborNode.gScore + (destCoord - neighbor).Mag());
                }

            }

        }

        return new List<Vector3>();

    }

    static bool LineOfSight(Coord a, Coord b)
    {
        if (a.x == b.x)
        {
            for (int i = a.z; i != b.z; i += (a.z - b.z > 0) ? -1 : 1)
            {
                if (Grid.tiledata[i][a.x].value != '.') return false;
            }
            return true;
        }
        if (a.x > b.x)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        float slope = (b.z - a.z) / (b.x - a.x);
        float curZ = a.z;
        bool isUp = slope > 0;
        for (int i = a.x; i < b.x; i++)
        {
            float newCurZ = curZ + .5f * slope;
            for (int j = Mathf.RoundToInt(curZ); isUp ? j <= Mathf.RoundToInt(newCurZ) : j >= Mathf.RoundToInt(newCurZ); j += isUp ? 1 : -1)
            {
                if (Grid.tiledata[j][i].value != '.') return false;

            }
            curZ = newCurZ;
            newCurZ = curZ + .5f * slope;
            for (int j = Mathf.RoundToInt(curZ); isUp ? j <= Mathf.RoundToInt(newCurZ) : j >= Mathf.RoundToInt(newCurZ); j += isUp ? 1 : -1)
            {
                if (Grid.tiledata[j][i + 1].value != '.') return false;
            }
            curZ = newCurZ;
        }

        return true;

    }

    Vector3 GetRayPlaneIntersection(Ray ray, Vector3 plane_normal)
    {
        float t = -(plane_normal.Dot(ray.start)) / (plane_normal.Dot(ray.direction));
        return ray.start + t * ray.direction;
    }

    //IEnumerator MoveCharacter(Actor actor, List<Vector3> points, float mvmSpeed) //mvmSpeed should be a stat
    //{
    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        Vector3 dest = points[i];
    //        while (actor.GlobalTranslation != dest)
    //        {
    //            Vector3 disp = dest - actor.GlobalTranslation;
    //            if (disp.Length() < mvmSpeed)
    //            {
    //                actor.GlobalTranslation = dest;
    //                break;
    //            }
    //            actor.GlobalTranslation += mvmSpeed * disp.Normalized();
    //            yield return null;
    //        }
    //    }

    //}
}



class ThetaNode : FastPriorityQueueNode
{
    public int x { get { return coord.x; } }
    public int z { get { return coord.z; } }
    public float gScore = float.MaxValue;
    public Coord coord;
    public ThetaNode parent;

    public ThetaNode(Coord _coord, ThetaNode _parent)
    {
        coord = _coord;
        this.parent = _parent;
    }
}

