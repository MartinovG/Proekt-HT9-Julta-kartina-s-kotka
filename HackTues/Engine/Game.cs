﻿using HackTues.Controls;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace HackTues.Engine;

public interface ILevelLoader {
    void LoadMap(string name);
}
public interface ILobbyLoader {
    void LoadLobby();
}

public abstract class Game: IGame {
    public static string AssetsPath {
        get {
            if (File.Exists(".assets")) {
                var r = new StreamReader(".assets");
                var path = r.ReadToEnd().Trim();
                r.Close();
                if (path != "") return Path.GetFullPath(path);
            }

            var w = new StreamWriter(".assets");
            w.WriteLine("assets");
            w.Close();

            return Path.GetFullPath("assets");
        }
    }
    public Map Map { get; protected set; } = new();
    protected abstract Vector2 Friction { get; }
    public abstract Vector2 CameraPos { get; }
    public abstract Layer PlayerLayer { get; }
    public abstract Hitbox PlayerHitbox { get; }
    public Vector2 PlayerPosition {
        get => pos;
        set => pos = value;
    }
    public bool Switch { get; protected set; }

    protected Vector2 pos, vel;

    protected abstract Vector2 GetAcceleration(float detla, IController? controller);
    protected virtual void HitX() { }
    protected virtual void HitY() {  }

    public void AddLayers(SortedSet<Layer> layers) {
        Map.AddLayers(layers);
        layers.Add(PlayerLayer + PlayerPosition);
    }

    public void Render() {
        var set = new SortedSet<Layer>();
        Map.AddLayers(set);
        set.Add(PlayerLayer);

        foreach (var fg in set) {
            fg.Render();
        }
    }
    public virtual void Update(float delta, IController? controller) {
        var acc = GetAcceleration(delta, controller);

        vel += acc * delta;
        vel *= new Vector2(MathF.Pow(Friction.X, delta), MathF.Pow(Friction.Y, delta));

        pos.X += vel.X * delta;
        if (Map.CollidesWith(PlayerHitbox + pos)) {
            pos.X -= vel.X * delta;
            vel.X = 0;
            HitX();
        }

        pos.Y += vel.Y * delta;
        if (Map.CollidesWith(PlayerHitbox + pos)) {
            pos.Y -= vel.Y * delta;
            vel.Y = 0;
            HitY();
        }
    }
}

public enum Direction {
    North,
    South,
    West,
    East,
}
public enum TurnDirection {
    Left,
    Right,
}
public enum WeaponDirection {
    Up,
    Middle,
    Down,
}

public class LobbyGame: Game {
    private Direction direction = Direction.North;
    private float walkCycle = 0;

    private Layer[] east = {
        new("player-office-east-0", new(0, 0), new(34, 84), new(17, 75)),
        new("player-office-east-1", new(0, 0), new(34, 81), new(17, 75)),
        new("player-office-east-2", new(0, 0), new(34, 78), new(17, 75)),
    };
    private Layer[] west = {
        new("player-office-west-0", new(0, 0), new(34, 84), new(17, 75)),
        new("player-office-west-1", new(0, 0), new(34, 81), new(17, 75)),
        new("player-office-west-2", new(0, 0), new(34, 78), new(17, 75)),
    };
    private Layer[] north = {
        new("player-office-north-0", new(0, 0), new(45, 83), new(23, 77)),
        new("player-office-north-1", new(0, 0), new(39, 83), new(20, 77)),
        new("player-office-north-2", new(0, 0), new(40, 83), new(20, 77)),
    };
    private Layer[] south = {
        new("player-office-south-0", new(0, 0), new(45, 83), new(23, 77)),
        new("player-office-south-1", new(0, 0), new(39, 83), new(20, 77)),
        new("player-office-south-2", new(0, 0), new(40, 83), new(20, 77)),
    };

    protected override Vector2 Friction { get; } = new(0.000001f, 0.000001f);
    public override Layer PlayerLayer {
        get {
            int i = (int)walkCycle % 4;
            if (i == 2) i = 0;
            if (i == 3) i = 2;

            return direction switch {
                Direction.North => north[i],
                Direction.South => south[i],
                Direction.West => west[i],
                Direction.East => east[i],
                _ => null!,
            };
        }
    }
    public override Hitbox PlayerHitbox { get; } = new(new(-12, -5), new(24, 10));
    public override Vector2 CameraPos => PlayerPosition;
    public ILevelLoader LevelLoader { get; }

    protected override Vector2 GetAcceleration(float detla, IController? controller) {
        var acc = Vector2.Zero;
        if (controller == null)
            return acc;

        if (controller.Get(Button.Left)) {
            acc -= Vector2.UnitX;
            direction = Direction.West;
        }
        if (controller.Get(Button.Right)) {
            acc += Vector2.UnitX;
            direction = Direction.East;
        }
        if (controller.Get(Button.Up)) {
            acc -= Vector2.UnitY;
            direction = Direction.North;
        }
        if (controller.Get(Button.Down)) {
            acc += Vector2.UnitY;
            direction = Direction.South;
        }

        if (acc.Length > 1)
            acc.Normalize();
        acc *= 1000;

        return acc;
    }

    public override void Update(float delta, IController? controller) {
        base.Update(delta, controller);

        if (vel.Length > 1) {
            walkCycle += delta * 4;
        }
        else {
            walkCycle = 0;
        }

        if (controller?.Poll(Button.Shoot) == true) {
            foreach (var entry in Map.EntryLayers) {
                if ((pos - entry.Position).Length < 16) {
                    LevelLoader.LoadMap(entry.Texture[6..]);
                }
            }
        }
    }

    public LobbyGame(ILevelLoader lobbyLoader) {
        LevelLoader = lobbyLoader;
        Map = Map.Load(new FileStream(Path.Combine(AssetsPath, "maps/spawn"), FileMode.Open));
        PlayerPosition = Map.Spawn;
    }
}
public class LevelGame: Game {
    private Layer[] left = {
        new("player-map-left-0-0", new(0, 0), new(36, 63), new(19, 61)),
        new("player-map-left-0-1", new(0, 0), new(42, 63), new(22, 61)),
        new("player-map-left-0-2", new(0, 0), new(42, 63), new(21, 61)),
        new("player-map-left-1-0", new(0, 0), new(36, 67), new(19, 61)),
        new("player-map-left-1-1", new(0, 0), new(42, 67), new(22, 61)),
        new("player-map-left-1-2", new(0, 0), new(42, 67), new(21, 61)),
    };
    private Layer[] right = {
        new("player-map-right-0-0", new(0, 0), new(36, 63), new(19, 61)),
        new("player-map-right-0-1", new(0, 0), new(42, 63), new(22, 61)),
        new("player-map-right-0-2", new(0, 0), new(42, 63), new(21, 61)),
        new("player-map-right-1-0", new(0, 0), new(36, 67), new(19, 61)),
        new("player-map-right-1-1", new(0, 0), new(42, 67), new(22, 61)),
        new("player-map-right-1-2", new(0, 0), new(42, 67), new(21, 61)),
    };
    private TurnDirection turn = TurnDirection.Right;
    private WeaponDirection weapon = WeaponDirection.Middle;
    private float walkCycle = 0;

    protected override Vector2 Friction { get; } = new(0.000001f, 0.1f);
    public override Vector2 CameraPos => new(Math.Max(1440 / 4, pos.X), 900 / 4);
    public override Layer PlayerLayer {
        get {
            int i = (int)walkCycle % 2;

            return turn switch {
                TurnDirection.Right => right[(int)weapon + i * 3],
                TurnDirection.Left => left[(int)weapon + i * 3],
                _ => null!,
            };
        }
    }
    public override Hitbox PlayerHitbox { get; } = new(new(-16, -64), new(32, 64));
    public ILevelLoader LevelLoader { get; }

    public void LoadMap(string name) {
        Map = Map.Load(new FileStream(Path.Combine(AssetsPath, "maps/" + name), FileMode.Open));
    }

    private float kyoteeTime = 0;
    private float jumpCooldown = 0;

    protected override void HitY() {
        kyoteeTime = .1f;
    }
    protected override Vector2 GetAcceleration(float detla, IController? controller) {
        var acc = Vector2.Zero;
        if (controller == null)
            return acc;
        if (controller.Get(Button.Left)) {
            acc -= Vector2.UnitX * 1250;
            turn = TurnDirection.Left;
        }
        if (controller.Get(Button.Right)) {
            acc += Vector2.UnitX * 1250;
            turn = TurnDirection.Right;
        }
        if (controller.Get(Button.Up) && kyoteeTime > 0 && jumpCooldown < 0 && controller.Poll(Button.Up)) {
            vel.Y = -400;
            kyoteeTime = 0;
            jumpCooldown = .5f;
        }

        if (controller.Get(Button.WeaponLeft)) {
            turn = TurnDirection.Left;
            weapon = WeaponDirection.Middle;
        }
        if (controller.Get(Button.WeaponRight)) {
            turn = TurnDirection.Right;
            weapon = WeaponDirection.Middle;
        }
        if (controller.Get(Button.WeaponUp)) {
            weapon = WeaponDirection.Up;
        }
        if (controller.Get(Button.WeaponDown)) {
            weapon = WeaponDirection.Down;
        }

        acc += Vector2.UnitY * 700;

        return acc;
    }

    public override void Update(float delta, IController? controller) {
        base.Update(delta, controller);

        if (Math.Abs(vel.X) > 1) {
            walkCycle += delta * 4;
        }
        else {
            walkCycle = 0;
        }

        jumpCooldown -= delta;
        kyoteeTime -= delta;
    }

    public LevelGame(ILevelLoader levelLoader) {
        LevelLoader = levelLoader;
    }
}
