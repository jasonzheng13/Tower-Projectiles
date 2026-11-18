using UnityEngine;

namespace MechTD
{
    /// <summary>
    /// An enemy unit moving along the predetermined route. Uses a kinematic Rigidbody so
    /// physics shells collide with it. Exposes its velocity so towers can lead their shots.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyStats Stats { get; private set; }
        public float Hp { get; private set; }
        public float Distance { get; private set; }   // meters traveled along the route
        public Vector3 Velocity { get; private set; } // current world velocity (m/s)
        public bool IsDead { get; private set; }

        /// <summary>Center of the hit box: what towers aim at.</summary>
        public Vector3 AimPoint => transform.position + Vector3.up * aimHeight;

        private Rigidbody rb;
        private HealthBar healthBar;
        private float aimHeight;
        private float launchTimer;
        private Transform visual;
        private Vector3 visualScale;
        private float flashTimer;

        public static Enemy Spawn(EnemyKind kind, float startDistance)
        {
            EnemyStats stats = EnemyStats.Get(kind);
            GameObject visual = BuildVisual(stats);
            var root = new GameObject(stats.Name);
            visual.transform.SetParent(root.transform, false);

            // One box collider around the whole model
            Bounds b = Visuals.RendererBounds(visual);
            var col = root.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = b.size;

            var enemy = root.AddComponent<Enemy>();
            enemy.Stats = stats;
            enemy.Hp = stats.MaxHp;
            enemy.Distance = startDistance;
            enemy.aimHeight = b.center.y;
            enemy.visual = visual.transform;
            enemy.visualScale = visual.transform.localScale;
            enemy.healthBar = Visuals.AddHealthBar(root.transform, b.max.y + 0.8f, Mathf.Clamp(stats.Size * 0.6f, 1.6f, 6f));
            enemy.launchTimer = 4f;

            enemy.rb = root.AddComponent<Rigidbody>();
            enemy.rb.isKinematic = true;
            enemy.rb.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 pos = GameManager.Instance.Path.Evaluate(startDistance, out Vector3 dir);
            root.transform.SetPositionAndRotation(pos + Vector3.up * stats.FlyHeight, Quaternion.LookRotation(dir));
            enemy.Velocity = dir * stats.Speed;
            return enemy;
        }

        private static GameObject BuildVisual(EnemyStats stats)
        {
            Color crimson = new Color(0.55f, 0.08f, 0.1f);
            Color gunmetal = new Color(0.2f, 0.21f, 0.24f);
            Color orange = new Color(1f, 0.45f, 0.1f);

            switch (stats.Kind)
            {
                case EnemyKind.ScoutDrone:
                {
                    GameObject m = Visuals.LoadModel("enemy-ufo-a", stats.Size, false);
                    if (m == null)
                    {
                        m = new GameObject("Drone");
                        Visuals.Prim(PrimitiveType.Sphere, m.transform, new Vector3(0, 0.4f, 0), new Vector3(2f, 0.6f, 2f), Visuals.Lit(gunmetal));
                        Visuals.Prim(PrimitiveType.Sphere, m.transform, new Vector3(0, 0.7f, 0), new Vector3(0.8f, 0.5f, 0.8f), Visuals.Lit(orange, 2f));
                    }
                    var spin = m.AddComponent<Spinner>();
                    spin.DegreesPerSecond = 180f;
                    spin.BobHeight = 0.25f;
                    return m;
                }
                case EnemyKind.AssaultMech:
                {
                    var mech = Visuals.BuildMech("Assault Mech", stats.Size, gunmetal, crimson, orange, orange, true);
                    Visuals.AddRifle(mech, 0.5f, Visuals.Lit(new Color(0.12f, 0.12f, 0.13f)), Visuals.Lit(orange, 1f));
                    AddWalker(mech, 1.6f);
                    return mech.Root.gameObject;
                }
                case EnemyKind.HeavyMech:
                {
                    var mech = Visuals.BuildMech("Juggernaut", stats.Size, crimson, gunmetal, orange, new Color(1f, 0.15f, 0.1f), true, 1.35f, 0.85f);
                    // Shoulder missile pods
                    for (int side = -1; side <= 1; side += 2)
                        Visuals.Box(mech.Body, new Vector3(0.75f * side, 1.55f, -0.05f), new Vector3(0.3f, 0.3f, 0.5f), Visuals.Lit(gunmetal));
                    AddWalker(mech, 0.9f);
                    return mech.Root.gameObject;
                }
                default: // Mothership
                {
                    GameObject m = Visuals.LoadModel("craft_cargoA", stats.Size, false);
                    if (m == null)
                    {
                        m = new GameObject("Mothership");
                        Visuals.Prim(PrimitiveType.Capsule, m.transform, new Vector3(0, 1.5f, 0), new Vector3(4f, 7f, 4f), Visuals.Lit(gunmetal), new Vector3(90, 0, 0));
                        Visuals.Box(m.transform, new Vector3(0, 2.6f, -2f), new Vector3(2f, 1.4f, 5f), Visuals.Lit(crimson));
                    }
                    // Glowing hangar lights so it reads as a carrier
                    Bounds b = Visuals.RendererBounds(m);
                    for (int i = -1; i <= 1; i++)
                        Visuals.Prim(PrimitiveType.Sphere, m.transform, new Vector3(i * b.extents.x * 0.5f, 0.1f, 0f),
                                     Vector3.one * 0.8f, Visuals.Lit(orange, 4f), null, "HangarLight");
                    return m;
                }
            }
        }

        private static void AddWalker(Visuals.MechParts mech, float stepsPerSecond)
        {
            var walker = mech.Root.gameObject.AddComponent<MechWalker>();
            walker.LeftLeg = mech.LeftLeg;
            walker.RightLeg = mech.RightLeg;
            walker.Body = mech.Body;
            walker.StepsPerSecond = stepsPerSecond;
        }

        void FixedUpdate()
        {
            GameManager gm = GameManager.Instance;
            if (IsDead || gm == null || gm.State != GameState.Playing) return;

            Distance += Stats.Speed * Time.fixedDeltaTime;
            if (Distance >= gm.Path.Length)
            {
                gm.OnEnemyReachedBase(this);
                return;
            }

            Vector3 pos = gm.Path.Evaluate(Distance, out Vector3 dir) + Vector3.up * Stats.FlyHeight;
            Velocity = dir * Stats.Speed;
            rb.MovePosition(pos);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(dir), 240f * Time.fixedDeltaTime));

            // Motherships launch scout drones as they fly over the route
            // (but not near the end of the route, where a fast drone would get a free run at the base)
            if (Stats.Kind == EnemyKind.Mothership && Distance < gm.Path.Length * 0.7f)
            {
                launchTimer -= Time.fixedDeltaTime;
                if (launchTimer <= 0f)
                {
                    launchTimer = 7f;
                    gm.SpawnEnemy(EnemyKind.ScoutDrone, Distance);
                }
            }
        }

        void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f) SetFlash(false);
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            Hp -= amount;
            healthBar.Set(Hp / Stats.MaxHp);
            flashTimer = 0.08f;
            SetFlash(true);
            if (Hp <= 0f) Die();
        }

        private void SetFlash(bool on)
        {
            // Hit flash: briefly scale the model up a touch instead of swapping materials
            if (visual != null) visual.localScale = on ? visualScale * 1.06f : visualScale;
        }

        private void Die()
        {
            IsDead = true;
            float size = Stats.Size;
            Visuals.Burst(AimPoint, size * 0.7f, new Color(1f, 0.55f, 0.15f), 0.6f);
            if (Stats.Kind == EnemyKind.Mothership)
            {
                for (int i = 0; i < 4; i++)
                    Visuals.Burst(AimPoint + Random.insideUnitSphere * size * 0.4f, size * 0.4f, new Color(1f, 0.8f, 0.3f), 0.9f, false);
            }
            GameManager.Instance.OnEnemyKilled(this);
            Destroy(gameObject);
        }
    }
}
