using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MechTD
{
    public enum GameState { Playing, Defeat, Victory }

    /// <summary>
    /// Runs the match: builds the battlefield, spawns waves, tracks deployed mechs,
    /// score and win/lose state. Created automatically by <see cref="GameBootstrap"/>.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;
        public EnemyPath Path { get; private set; }
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<MechTower> Towers = new List<MechTower>();

        public int TowerLimit { get; private set; } = 6;
        public int TowersRemaining => TowerLimit - Towers.Count;
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int WaveNumber { get; private set; }        // 0 before the first wave
        public int TotalWaves => Waves.Length;
        public bool WaveInProgress { get; private set; }
        public float Countdown { get; private set; }
        public string Message { get; private set; } = "";
        public string DefeatReason { get; private set; } = "";
        public bool FastForward { get; private set; }

        private float messageTimer;
        private bool launchNow;
        private readonly List<(Vector3 pos, float radius)> blockers = new List<(Vector3, float)>();

        public const float MapHalfSize = 48f;
        public const int MaxTowerLimit = 14;

        // ---------- Waves ----------

        private struct Group
        {
            public EnemyKind Kind; public int Count; public float Interval;
            public Group(EnemyKind k, int c, float i) { Kind = k; Count = c; Interval = i; }
        }

        private static readonly Group[][] Waves =
        {
            new[] { new Group(EnemyKind.ScoutDrone, 6, 1.4f) },
            new[] { new Group(EnemyKind.ScoutDrone, 6, 1.0f), new Group(EnemyKind.AssaultMech, 3, 2.5f) },
            new[] { new Group(EnemyKind.AssaultMech, 6, 2.0f), new Group(EnemyKind.ScoutDrone, 8, 0.8f) },
            new[] { new Group(EnemyKind.HeavyMech, 2, 4f), new Group(EnemyKind.AssaultMech, 6, 1.6f) },
            new[] { new Group(EnemyKind.Mothership, 1, 1f), new Group(EnemyKind.ScoutDrone, 8, 1.0f) },
            new[] { new Group(EnemyKind.ScoutDrone, 10, 0.6f), new Group(EnemyKind.HeavyMech, 3, 3f), new Group(EnemyKind.AssaultMech, 8, 1.2f) },
            new[] { new Group(EnemyKind.Mothership, 2, 10f), new Group(EnemyKind.HeavyMech, 4, 2.5f) },
            new[] { new Group(EnemyKind.AssaultMech, 10, 1.0f), new Group(EnemyKind.HeavyMech, 4, 2f), new Group(EnemyKind.Mothership, 3, 6f) },
        };

        // ---------- Setup ----------

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Physics.IgnoreLayerCollision(Shell.Layer, Shell.Layer, true);

            RemoveLegacyPrototype();
            BuildBattlefield();

            gameObject.AddComponent<TowerPlacer>();
            gameObject.AddComponent<GameHud>();

            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<MainCamera>() == null) cam.gameObject.AddComponent<MainCamera>();
        }

        void Start() => StartCoroutine(RunWaves());

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>The scene still holds the in-class prototype (buggies + click-to-fire tower). Remove it.</summary>
        private static void RemoveLegacyPrototype()
        {
            foreach (var t in FindObjectsByType<Tower_Base>()) Destroy(t.gameObject);
            foreach (var b in FindObjectsByType<Buggy>()) Destroy(b.gameObject);
            foreach (var s in FindObjectsByType<Score>()) Destroy(s.gameObject);
        }

        private void BuildBattlefield()
        {
            // Winding route: enemies enter through the jump gate (west) and head for the base (east)
            Path = new EnemyPath(new[]
            {
                new Vector3(-46, 0, 36), new Vector3(-24, 0, 36), new Vector3(-24, 0, 8),
                new Vector3(-38, 0, 8),  new Vector3(-38, 0, -30), new Vector3(-6, 0, -30),
                new Vector3(-6, 0, 22),  new Vector3(20, 0, 22),  new Vector3(20, 0, -20),
                new Vector3(40, 0, -20), new Vector3(40, 0, 30),
            });

            var world = new GameObject("Battlefield").transform;

            // Ground: dark lunar surface
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                ground.transform.position = new Vector3(0, -0.05f, 0);
                ground.transform.localScale = new Vector3(100, 0.1f, 100);
            }
            ground.GetComponent<Renderer>().sharedMaterial = Visuals.Lit(new Color(0.23f, 0.25f, 0.29f), 0f, 0.1f, 0.25f);

            // Route surface with glowing edges
            Material road = Visuals.Lit(new Color(0.12f, 0.12f, 0.14f), 0f, 0.4f, 0.6f);
            Material edge = Visuals.Lit(new Color(1f, 0.35f, 0.1f), 2.5f);
            float w = EnemyPath.HalfWidth * 2f;
            for (int i = 1; i < Path.Points.Length; i++)
            {
                Vector3 a = Path.Points[i - 1], b = Path.Points[i];
                Vector3 mid = (a + b) / 2f;
                float len = Vector3.Distance(a, b);
                Quaternion rot = Quaternion.LookRotation(b - a);
                var seg = Visuals.Box(world, mid + Vector3.up * 0.02f, new Vector3(w, 0.04f, len + w), road, "Route");
                seg.rotation = rot;
                for (int side = -1; side <= 1; side += 2)
                {
                    var strip = Visuals.Box(world, mid + rot * new Vector3(side * EnemyPath.HalfWidth, 0.06f, 0), new Vector3(0.15f, 0.05f, len), edge, "RouteEdge");
                    strip.rotation = rot;
                }
                // Chevrons pointing in the direction of travel
                for (float d = 4f; d < len - 2f; d += 6f)
                {
                    Vector3 p = a + (b - a).normalized * d + Vector3.up * 0.05f;
                    var chev = Visuals.Box(world, p, new Vector3(1.4f, 0.03f, 0.35f), Visuals.Lit(new Color(0.9f, 0.3f, 0.1f), 1.2f), "Chevron");
                    chev.rotation = rot;
                }
            }

            // Enemy jump gate at the start, Federation base at the end
            Vector3 start = Path.Points[0], end = Path.Points[Path.Points.Length - 1];
            PlaceModel(world, "gate_complex", start + Vector3.left * 1.5f, 7f, true, 90f, 5f);
            GameObject hangar = PlaceModel(world, "hangar_largeA", end + Vector3.forward * 6f, 14f, false, 180f, 9f);
            if (hangar == null)
                Visuals.Box(world, end + Vector3.forward * 6f + Vector3.up * 3f, new Vector3(12, 6, 10), Visuals.Lit(new Color(0.8f, 0.82f, 0.86f)), "Base");
            PlaceModel(world, "satelliteDish_large", end + new Vector3(-9f, 0, 9f), 6f, true, 200f, 3.5f);
            PlaceModel(world, "machine_generatorLarge", end + new Vector3(8f, 0, 4f), 3.5f, true, 0f, 2.5f);

            // Scenery (fixed seed so the layout is the same every game)
            var rng = new System.Random(381);
            string[] props = { "rock_largeA", "rock_largeB", "rock_crystals", "rock_crystalsLargeA", "craterLarge", "crater", "meteor_detailed", "detail-crystal-large" };
            int placed = 0;
            for (int tries = 0; tries < 400 && placed < 26; tries++)
            {
                var p = new Vector3((float)(rng.NextDouble() * 2 - 1) * MapHalfSize, 0, (float)(rng.NextDouble() * 2 - 1) * MapHalfSize);
                float size = 1.5f + (float)rng.NextDouble() * 2.5f;
                if (Path.DistanceTo(p) < EnemyPath.HalfWidth + size + 1f) continue;
                if (IsBlocked(p, size + 2f)) continue;
                string prop = props[rng.Next(props.Length)];
                bool flat = prop.StartsWith("crater");
                if (PlaceModel(world, prop, p, flat ? size * 2.2f : size, !flat, (float)rng.NextDouble() * 360f, flat ? 0f : size * 0.6f) != null)
                    placed++;
            }
        }

        private GameObject PlaceModel(Transform parent, string model, Vector3 pos, float size, bool fitHeight, float yaw, float blockRadius)
        {
            GameObject go = Visuals.LoadModel(model, size, fitHeight);
            if (go == null) return null;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            if (blockRadius > 0f) blockers.Add((pos, blockRadius));
            return go;
        }

        private bool IsBlocked(Vector3 p, float radius)
        {
            foreach (var b in blockers)
                if ((b.pos - p).sqrMagnitude < (b.radius + radius) * (b.radius + radius)) return true;
            return false;
        }

        // ---------- Tower placement ----------

        public bool CanPlaceAt(Vector3 p, out string reason)
        {
            if (TowersRemaining <= 0) { reason = "No mechs left to deploy - clear a wave to earn another"; return false; }
            if (Mathf.Abs(p.x) > MapHalfSize || Mathf.Abs(p.z) > MapHalfSize) { reason = "Outside the battlefield"; return false; }
            if (Path.DistanceTo(p) < EnemyPath.HalfWidth + 1.8f) { reason = "Can't deploy on the enemy route"; return false; }
            foreach (MechTower t in Towers)
                if (Vector3.Distance(t.transform.position, p) < 4f) { reason = "Too close to another mech"; return false; }
            if (IsBlocked(p, 1.2f)) { reason = "Blocked by terrain"; return false; }
            reason = "";
            return true;
        }

        public MechTower PlaceTower(TowerKind kind, Vector3 position)
        {
            if (!CanPlaceAt(position, out _)) return null;
            // Face the nearest part of the route
            Vector3 nearest = position;
            float best = float.MaxValue;
            for (float d = 0; d < Path.Length; d += 2f)
            {
                Vector3 q = Path.Evaluate(d, out _);
                float dist = (q - position).sqrMagnitude;
                if (dist < best) { best = dist; nearest = q; }
            }
            Vector3 look = nearest - position; look.y = 0;
            MechTower tower = MechTower.Deploy(kind, position, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity);
            Towers.Add(tower);
            return tower;
        }

        // ---------- Enemies ----------

        public Enemy SpawnEnemy(EnemyKind kind, float distance = 0f)
        {
            Enemy e = Enemy.Spawn(kind, distance);
            Enemies.Add(e);
            return e;
        }

        public void OnEnemyKilled(Enemy e)
        {
            Enemies.Remove(e);
            Score += e.Stats.Score;
            Kills++;
        }

        public void OnEnemyReachedBase(Enemy e)
        {
            if (State != GameState.Playing) return;
            DefeatReason = $"A {e.Stats.Name} broke through to the Federation base!";
            State = GameState.Defeat;
            Time.timeScale = 0f;
        }

        private IEnumerator RunWaves()
        {
            for (int w = 0; w < Waves.Length; w++)
            {
                // Build phase: countdown before the wave (SPACE launches early)
                Countdown = w == 0 ? 20f : 10f;
                launchNow = false;
                while (Countdown > 0f && !launchNow)
                {
                    Countdown -= Time.deltaTime;
                    yield return null;
                }
                Countdown = 0f;

                WaveNumber = w + 1;
                WaveInProgress = true;
                ShowMessage($"WAVE {WaveNumber} INBOUND", 2.5f);
                foreach (Group g in Waves[w])
                {
                    for (int i = 0; i < g.Count; i++)
                    {
                        SpawnEnemy(g.Kind);
                        yield return new WaitForSeconds(g.Interval);
                    }
                }

                while (Enemies.Count > 0) yield return null;
                WaveInProgress = false;

                if (w < Waves.Length - 1)
                {
                    if (TowerLimit < MaxTowerLimit)
                    {
                        TowerLimit++;
                        ShowMessage($"WAVE {WaveNumber} CLEARED  -  +1 mech deployment authorized", 4f);
                    }
                    else ShowMessage($"WAVE {WaveNumber} CLEARED", 3f);
                }
            }

            State = GameState.Victory;
            Time.timeScale = 0f;
        }

        public void ShowMessage(string text, float seconds)
        {
            Message = text;
            messageTimer = seconds;
        }

        void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f) Message = "";
            }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (State != GameState.Playing)
            {
                if (kb.rKey.wasPressedThisFrame) Restart();
                return;
            }
            if (kb.spaceKey.wasPressedThisFrame && !WaveInProgress) launchNow = true;
            if (kb.fKey.wasPressedThisFrame)
            {
                FastForward = !FastForward;
                Time.timeScale = FastForward ? 2f : 1f;
            }
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
