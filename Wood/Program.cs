using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
/**
 * Codingame Fall Challenge 2023 - Wood league Client
 * Objective: Score points by scanning valuable fish faster than your opponent.
 * Rules:
 *   The game is played turn by turn. Each turn, each player gives an action for their drone to perform.
 * 
 * The Map:
 *   The map is a square of 10,000 units on each side. Length units will be denoted as "u" in the rest of the statement.
 *   The coordinate (0, 0) is located at the top left corner of the map.
 * 
 * Drones:
 *   Each player has to explore the ocean floor and scan the fish. Each turn, the player can decide to move their drone
 *   in a direction or not activate its motors.
 * 
 *   Your drone continuously emits light around it. If a fish is within this light radius, it is automatically scanned.
 *   You can increase the power of your light (and thus your scan radius), but this will drain your battery.
 **/
class Program
{
    private const string Version = "0.0.1";
    private const string Name = "FallWood2023";
    private const string Description = "Wood league client for the Fall Challenge 2023";
    private const string Author = "KodeMonkey";
    static void Main(string[] args)
    {
        CgIo.Info($"{Name} {Version} - {Description}");
        CgIo.Info($"Author: {Author}");
        var brain = new BotBrain();
        brain.Initialize();
        while (true)
        {
            foreach(var action in brain.GetAction()){
                if(action.Type == ActionType.Quit){
                    return;
                }
                CgIo.Out.WriteLine($"{action}");
            }
        }
    }
}

#region CgIo
//Codingame IO, This will allow testing if needed
public static class CgIo
{
    public static bool LogInput { get; set; } = true;
    public static bool LogDebug { get; set; } = false;
    public static bool LogInfo { get; set; } = true;
    public static bool LogError { get; set; } = true;
    public static TextWriter Out { get; private set; }
    private static TextWriter Log { get; set; }
    public static TextReader In { get; private set; }
    
    /// <summary>
    /// Gets the next line from the configured input stream.
    /// </summary>
    /// <returns>a string</returns>
    public static string Next()
    {
        var line = In.ReadLine();
        return line ?? "";
    }

    static CgIo()
    {
        Out = Console.Out;
        Log = Console.Error;
        In = Console.In;
    }

    public static void Debug(string s)
    {
        if (!LogDebug)
            return;
        Log.WriteLine($"DEBUG: {s}");
    }

    public static void Input(string i)
    {
        if (!LogInput)
            return;
        Log.WriteLine(i);
    }

    public static void Info(string s)
    {
        if (!LogInfo)
            return;
        Log.WriteLine($"INFO: {s}");
    }

    public static void Error(string s)
    {
        if (!LogError)
            return;
        Log.WriteLine($"Error: {s}");
    }

    public static void Error(string s, Exception e)
    {
        if (!LogError)
            return;
        Log.WriteLine($"Error: {s}");
        Log.WriteLine($"Exception: {e.Message}");
        Log.WriteLine(($"Stacktrace: {e.StackTrace}"));
    }

    public static void SetOut(TextWriter w)
    {
        Out = w;
    }

    public static void SetLog(TextWriter w)
    {
        Log = w;
    }

    public static void SetIn(TextReader r)
    {
        In = r;
    }
}

#endregion

#region Game Interfaces

public interface IGameObject
{
    int Id { get; set; }
    int X { get; set; }
    int Y { get; set; }
}

public interface IAiStrategy
{
    Action[] GetActions(GameState state);
    string Name { get; }
}

#endregion

#region GameState
//This is the game state, it will be updated each turn
public class GameState
{
    public int MyScore { get; set; }
    public int FoeScore { get; set; }
    public int MyScanCount { get; set; }
    public int FoeScanCount { get; set; }
    public int CreatureCount { get; set; }
    public int MyDroneCount { get; set; }
    public int FoeDroneCount { get; set; }
    public int DroneScanCount { get; set; }
    public int VisibleCreatureCount { get; set; }
    public int RadarBlipCount { get; set; }
    public List<Creature> Creatures { get; set; } = new List<Creature>();
    public List<Drone> MyDrones { get; set; } = new List<Drone>();
    public List<Drone> FoeDrones { get; set; } = new List<Drone>();
    
    public HashSet<int> MyScans { get; set; } = new HashSet<int>();
    public HashSet<int> FoeScans { get; set; } = new HashSet<int>();
}
#endregion

#region Drone
// this is the drone state class
public class Drone : IGameObject
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Emergency { get; set; }
    public int Battery { get; set; }
}
#endregion

#region Creature
// create creature state class
public class Creature : IGameObject
{
    public const int TYPE_0_MIN_Y = 2500;
    public const int TYPE_0_MAX_Y = 5000;
    public const int TYPE_1_MIN_Y = 5000;
    public const int TYPE_1_MAX_Y = 7500;
    public const int TYPE_2_MIN_Y = 7500;
    public const int TYPE_2_MAX_Y = 10000;
    public const int TYPE_0_POINTS = 1; 
    public const int TYPE_1_POINTS = 2;
    public const int TYPE_2_POINTS = 3;
    
    public int Id { get; set; }
    public int Color { get; set; }
    public int Type { get; set; }
    
    public bool IsVisible { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Vx { get; set; }
    public int Vy { get; set; }
    public int TargetX => X + Vx;
    public int TargetY => Y + Vy;
    public bool IsScanned => IsScannedByMe || IsScannedByFoe;
    
    public bool IsScannedByMe { get; set; }
    public bool IsScannedByFoe { get; set; }
    
    public void Reset()
    {
        IsVisible = false;
        IsScannedByFoe = false;
        IsScannedByMe = false;
        X = -1;
        Y = -1;
        Vx = 0;
        Vy = 0;
    }
    
    public int MinY => Type == 0 ? TYPE_0_MIN_Y : Type == 1 ? TYPE_1_MIN_Y : TYPE_2_MIN_Y;
    public int MaxY => Type == 0 ? TYPE_0_MAX_Y : Type == 1 ? TYPE_1_MAX_Y : TYPE_2_MAX_Y;
    public int TypePoints => Type == 0 ? TYPE_0_POINTS : Type == 1 ? TYPE_1_POINTS : TYPE_2_POINTS;
    public int Points => IsScanned ? TypePoints : TypePoints * 2;
}
#endregion

#region RadarBlip
// this is the radar blip state class
public class RadarBlip
{
    public int DroneId { get; set; }
    public int CreatureId { get; set; }
    public string Radar { get; set; }
}
#endregion

#region Action
// this is the action class
public enum ActionType
{
    Move,
    Wait,
    Quit
}
public class Action
{
    public ActionType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Light { get; set; }
    
    public string Message { get; set; }
    public override string ToString()
    {
        var l = Light ? 1 : 0;
        if(Type == ActionType.Quit){
            return "Quit";
        }
        if (Type==ActionType.Wait){
            return $"WAIT {l} {Message}";
        }
        return $"MOVE {X} {Y} {l} {Message}";
    }
}
#endregion

#region BotBrain
// this is the bot brain class
public class BotBrain
{
    public IAiStrategy Strategy { get; set; } = new WoodStrategy();
    public GameState State { get; set; } = new GameState();
    private static string NextLine()
    {
        var line = CgIo.Next();
        CgIo.Input(line);
        return line;
    }
    
    public void Initialize()
    {
        string[] inputs;
        State.CreatureCount = int.Parse(NextLine());
        for (var i = 0; i < State.CreatureCount; i++)
        {
            inputs = NextLine().Split(' ');
            var creatureId = int.Parse(inputs[0]);
            var color = int.Parse(inputs[1]);
            var type = int.Parse(inputs[2]);
            State.Creatures.Add(new Creature
            {
                Id = creatureId,
                Color = color,
                Type = type
            });
        }
    }

    public void Update()
    {
        State.Creatures.ForEach(c => c.Reset());
        
        string[] inputs;
        State.MyScore = int.Parse(NextLine());
        State.FoeScore = int.Parse(NextLine());
        
        State.MyScanCount = int.Parse(NextLine());
        for (var i = 0; i < State.MyScanCount; i++)
        {
            var creatureId = int.Parse(NextLine());
            State.MyScans.Add(creatureId);
        }

        State.FoeScanCount = int.Parse(NextLine());
        for (var i = 0; i < State.FoeScanCount; i++)
        {
            var creatureId = int.Parse(NextLine());
            State.FoeScans.Add(creatureId);
        }

        // Update my drones
        State.MyDrones.Clear();
        State.MyDroneCount = int.Parse(NextLine());
        for (var i = 0; i < State.MyDroneCount; i++)
        {
            inputs = NextLine().Split(' ');
            var droneId = int.Parse(inputs[0]);
            var droneX = int.Parse(inputs[1]);
            var droneY = int.Parse(inputs[2]);
            var emergency = int.Parse(inputs[3]);
            var battery = int.Parse(inputs[4]);
            State.MyDrones.Add(new Drone
            {
                Id = droneId,
                X = droneX,
                Y = droneY,
                Emergency = emergency,
                Battery = battery,
            });
        }
        
        // Update foe drones
        State.FoeDrones.Clear();
        State.FoeDroneCount = int.Parse(NextLine());
        for (var i = 0; i < State.FoeDroneCount; i++)
        {
            inputs = NextLine().Split(' ');
            var droneId = int.Parse(inputs[0]);
            var droneX = int.Parse(inputs[1]);
            var droneY = int.Parse(inputs[2]);
            var emergency = int.Parse(inputs[3]);
            var battery = int.Parse(inputs[4]);
            State.FoeDrones.Add(new Drone
            {
                Id = droneId,
                X = droneX,
                Y = droneY,
                Emergency = emergency,
                Battery = battery
            });
        }

        State.DroneScanCount = int.Parse(NextLine());
        for (var i = 0; i < State.DroneScanCount; i++)
        {
            inputs = NextLine().Split(' ');
            var droneId = int.Parse(inputs[0]);
            var creatureId = int.Parse(inputs[1]);
        }

        State.VisibleCreatureCount = int.Parse(NextLine());
        for (var i = 0; i < State.VisibleCreatureCount; i++)
        {
            inputs = NextLine().Split(' ');
            var creatureId = int.Parse(inputs[0]);
            var creatureX = int.Parse(inputs[1]);
            var creatureY = int.Parse(inputs[2]);
            var creatureVx = int.Parse(inputs[3]);
            var creatureVy = int.Parse(inputs[4]);
            var creature = State.Creatures.Find(c => c.Id == creatureId);
            if (creature != null)
            {
                creature.IsVisible = true;
                creature.X = creatureX;
                creature.Y = creatureY;
                creature.Vx = creatureVx;
                creature.Vy = creatureVy;
                creature.IsScannedByMe = State.MyScans.Contains(creatureId);
                creature.IsScannedByFoe = State.FoeScans.Contains(creatureId);
            }
        }
        
        State.RadarBlipCount = int.Parse(NextLine());
        for (var i = 0; i < State.RadarBlipCount; i++)
        {
            inputs = NextLine().Split(' ');
            var droneId = int.Parse(inputs[0]);
            var creatureId = int.Parse(inputs[1]);
            var radar = inputs[2];
        }
    }

    public Action[] GetAction()
    {
        Update();
        return Strategy.GetActions(State);
    }
}
#endregion

#region Extensions
// this is the extensions class
public static class Extensions
{
    public static double Distance(this IGameObject c1, IGameObject c2)
    {
        return Math.Sqrt(Math.Pow(c1.X - c2.X, 2) + Math.Pow(c1.Y - c2.Y, 2));
    }
    
    public static double TargetDistance(this IGameObject c1, Creature c2)
    {
        return Math.Sqrt(Math.Pow(c1.X - c2.TargetX, 2) + Math.Pow(c1.Y - c2.TargetY, 2));
    }
}
#endregion

public class WoodStrategy : IAiStrategy
{
    public string Name => "Wood";
    
    public Action[] GetActions(GameState state)
    {
        var actions = new List<Action>();
        foreach (var drone in state.MyDrones)
        {
            var fish = state.Creatures
                .Where(c => c.IsVisible && !c.IsScannedByMe)
                .MinBy(c => c.Distance(drone));
            
            if (fish != null)
            {
                var d = drone.TargetDistance(fish);
                var light = d - 600 < 2000 && drone.Battery >= 5;
                actions.Add(new Action
                {
                    Type = ActionType.Move,
                    X = fish.TargetX,
                    Y = fish.TargetY,
                    Light = light,
                    Message = $"Fish {fish.Id}"
                });
            }
            else
            {
                actions.Add(new Action
                {
                    Type = ActionType.Wait,
                    Light = false
                });
            }

        }
        return actions.ToArray();
    }
}