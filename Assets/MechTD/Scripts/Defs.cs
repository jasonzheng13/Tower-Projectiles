using UnityEngine;

namespace MechTD
{
    public enum TowerKind { Striker, Longshot, Bastion }
    public enum EnemyKind { ScoutDrone, AssaultMech, HeavyMech, Mothership }

    /// <summary>Stats for one type of defensive mech (tower).</summary>
    public class TowerStats
    {
        public TowerKind Kind;
        public string Name;
        public string Role;
        public float FireRate;        // shots per second
        public float ProjectileSpeed; // launch speed in m/s
        public float Height;          // overall tower height in meters (affects muzzle height)
        public float Range;           // horizontal targeting range in meters
        public float Damage;
        public float SplashRadius;    // 0 = single target
        public bool HighArc;          // artillery lobs shells on the high ballistic arc
        public Color Primary, Secondary, Accent, ShotColor;

        public static readonly TowerStats[] All =
        {
            new TowerStats
            {
                Kind = TowerKind.Striker, Name = "STRIKER", Role = "Assault rifle frame",
                FireRate = 2.5f, ProjectileSpeed = 34f, Height = 4.5f, Range = 22f, Damage = 9f,
                Primary = new Color(0.92f, 0.93f, 0.96f), Secondary = new Color(0.12f, 0.32f, 0.78f),
                Accent = new Color(0.95f, 0.78f, 0.15f), ShotColor = new Color(0.4f, 0.9f, 1f)
            },
            new TowerStats
            {
                Kind = TowerKind.Longshot, Name = "LONGSHOT", Role = "Sniper on a watchtower",
                FireRate = 0.7f, ProjectileSpeed = 70f, Height = 10f, Range = 40f, Damage = 45f,
                Primary = new Color(0.30f, 0.36f, 0.30f), Secondary = new Color(0.80f, 0.82f, 0.78f),
                Accent = new Color(0.20f, 0.95f, 0.45f), ShotColor = new Color(0.5f, 1f, 0.55f)
            },
            new TowerStats
            {
                Kind = TowerKind.Bastion, Name = "BASTION", Role = "Heavy artillery, splash",
                FireRate = 0.4f, ProjectileSpeed = 26f, Height = 3f, Range = 30f, Damage = 32f,
                SplashRadius = 4.5f, HighArc = true,
                Primary = new Color(0.85f, 0.55f, 0.15f), Secondary = new Color(0.22f, 0.22f, 0.25f),
                Accent = new Color(1f, 0.3f, 0.2f), ShotColor = new Color(1f, 0.6f, 0.2f)
            },
        };

        public static TowerStats Get(TowerKind kind) => All[(int)kind];
    }

    /// <summary>Stats for one type of enemy unit.</summary>
    public class EnemyStats
    {
        public EnemyKind Kind;
        public string Name;
        public float MaxHp;
        public float Speed;      // m/s along the route
        public float FlyHeight;  // height above the ground the unit travels at
        public float Size;       // visual size in meters
        public int Score;

        public static readonly EnemyStats[] All =
        {
            new EnemyStats { Kind = EnemyKind.ScoutDrone,  Name = "Scout Drone",    MaxHp = 30f,  Speed = 7.5f, FlyHeight = 1.4f, Size = 2.2f, Score = 10 },
            new EnemyStats { Kind = EnemyKind.AssaultMech, Name = "Assault Mech",   MaxHp = 100f, Speed = 4.5f, FlyHeight = 0f,   Size = 3.6f, Score = 25 },
            new EnemyStats { Kind = EnemyKind.HeavyMech,   Name = "Juggernaut",     MaxHp = 320f, Speed = 2.6f, FlyHeight = 0f,   Size = 5.2f, Score = 60 },
            new EnemyStats { Kind = EnemyKind.Mothership,  Name = "Carrier Mothership", MaxHp = 1100f, Speed = 2.2f, FlyHeight = 8f, Size = 15f, Score = 250 },
        };

        public static EnemyStats Get(EnemyKind kind) => All[(int)kind];
    }
}
