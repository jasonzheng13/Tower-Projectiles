using UnityEngine;

namespace MechTD
{
    /// <summary>
    /// A deployed defensive mech. Fires continuously at its set rate at the enemy furthest
    /// along the route, leading the target based on its current position and velocity.
    /// </summary>
    public class MechTower : MonoBehaviour
    {
        public TowerStats Stats { get; private set; }

        private Transform body;     // rotates to face the target
        private Transform muzzle;
        private Enemy target;
        private float cooldown;
        private float retargetTimer;

        /// <summary>Builds the visual for a tower type (also used for the placement preview).</summary>
        public static GameObject BuildVisual(TowerStats s, out Transform body, out Transform muzzle)
        {
            Material gunMat = Visuals.Lit(new Color(0.15f, 0.15f, 0.17f), 0f, 0.7f, 0.5f);
            Material accent = Visuals.Lit(s.Accent, 1f);
            Color visor = s.Kind == TowerKind.Longshot ? new Color(0.3f, 1f, 0.4f) : new Color(0.3f, 0.9f, 1f);

            switch (s.Kind)
            {
                case TowerKind.Longshot:
                {
                    // Sniper mech standing on a lattice watchtower -> tallest tower
                    var root = new GameObject("Longshot");
                    float deck = s.Height - 4.2f;
                    Material frame = Visuals.Lit(new Color(0.35f, 0.37f, 0.4f), 0f, 0.8f, 0.4f);
                    for (int x = -1; x <= 1; x += 2)
                    for (int z = -1; z <= 1; z += 2)
                        Visuals.Box(root.transform, new Vector3(x * 1.1f, deck / 2f, z * 1.1f), new Vector3(0.3f, deck, 0.3f), frame, "Pillar");
                    for (int i = 1; i <= 2; i++)
                    {
                        float y = deck * i / 3f;
                        Visuals.Box(root.transform, new Vector3(0, y, 1.1f), new Vector3(2.4f, 0.15f, 0.15f), frame, "Brace");
                        Visuals.Box(root.transform, new Vector3(0, y, -1.1f), new Vector3(2.4f, 0.15f, 0.15f), frame, "Brace");
                        Visuals.Box(root.transform, new Vector3(1.1f, y, 0), new Vector3(0.15f, 0.15f, 2.4f), frame, "Brace");
                        Visuals.Box(root.transform, new Vector3(-1.1f, y, 0), new Vector3(0.15f, 0.15f, 2.4f), frame, "Brace");
                    }
                    Visuals.Box(root.transform, new Vector3(0, deck, 0), new Vector3(2.8f, 0.25f, 2.8f), Visuals.Lit(s.Secondary), "Deck");
                    Visuals.Prim(PrimitiveType.Sphere, root.transform, new Vector3(1.3f, deck + 0.25f, 1.3f), Vector3.one * 0.25f, accent, null, "Beacon");

                    var mech = Visuals.BuildMech("Mech", 4f, s.Primary, s.Secondary, s.Accent, visor, false);
                    mech.Root.SetParent(root.transform, false);
                    mech.Root.localPosition = new Vector3(0, deck + 0.12f, 0);
                    muzzle = Visuals.AddRifle(mech, 1.5f, gunMat, accent);
                    body = mech.Body;
                    return root;
                }
                case TowerKind.Bastion:
                {
                    // Short, wide artillery mech with shoulder cannons
                    var mech = Visuals.BuildMech("Bastion", s.Height, s.Primary, s.Secondary, s.Accent, visor, false, 1.45f, 0.65f);
                    float hip = 0.65f;
                    Transform cannons = Visuals.Pivot(mech.Body, "Cannons", new Vector3(0, hip + 0.85f, -0.1f));
                    cannons.localRotation = Quaternion.Euler(-35f, 0, 0);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Visuals.Box(cannons, new Vector3(0.45f * side, 0, 0), new Vector3(0.32f, 0.32f, 0.6f), Visuals.Lit(s.Secondary), "CannonHousing");
                        Visuals.Prim(PrimitiveType.Cylinder, cannons, new Vector3(0.45f * side, 0, 0.6f), new Vector3(0.18f, 0.4f, 0.18f), gunMat, new Vector3(90, 0, 0), "CannonBarrel");
                    }
                    muzzle = Visuals.Pivot(cannons, "Muzzle", new Vector3(0, 0, 1.05f));
                    body = mech.Body;
                    return mech.Root.gameObject;
                }
                default:
                {
                    var mech = Visuals.BuildMech("Striker", s.Height, s.Primary, s.Secondary, s.Accent, visor, false);
                    muzzle = Visuals.AddRifle(mech, 0.6f, gunMat, accent);
                    // Shield on the left arm
                    Visuals.Box(mech.Body, new Vector3(-0.74f, 0.95f, 0.1f), new Vector3(0.08f, 0.7f, 0.45f), Visuals.Lit(s.Secondary), "Shield");
                    Visuals.Box(mech.Body, new Vector3(-0.79f, 1.05f, 0.1f), new Vector3(0.04f, 0.2f, 0.2f), accent, "ShieldMark");
                    body = mech.Body;
                    return mech.Root.gameObject;
                }
            }
        }

        public static MechTower Deploy(TowerKind kind, Vector3 position, Quaternion facing)
        {
            TowerStats stats = TowerStats.Get(kind);
            GameObject visual = BuildVisual(stats, out Transform body, out Transform muzzle);
            var root = new GameObject(stats.Name + " Tower");
            root.transform.SetPositionAndRotation(position, facing);
            visual.transform.SetParent(root.transform, false);

            var tower = root.AddComponent<MechTower>();
            tower.Stats = stats;
            tower.body = body;
            tower.muzzle = muzzle;
            tower.cooldown = 0.3f;
            Visuals.Burst(position + Vector3.up, 2.5f, stats.ShotColor, 0.5f);
            return tower;
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            cooldown = Mathf.Max(cooldown - Time.deltaTime, -0.05f);
            retargetTimer -= Time.deltaTime;
            if (target == null || target.IsDead || retargetTimer <= 0f || !InRange(target))
            {
                target = FindTarget(gm);
                retargetTimer = 0.25f;
            }
            if (target == null) return;

            // Lead the target: aim where it will be when the shell arrives
            if (!Ballistics.TryLeadTarget(muzzle.position, target.AimPoint, target.Velocity,
                                          Stats.ProjectileSpeed, Stats.HighArc,
                                          out Vector3 launchVelocity, out Vector3 aimPoint))
            {
                target = null;
                return;
            }

            // Turn the mech toward the predicted point
            Vector3 flat = aimPoint - body.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
            {
                Quaternion want = Quaternion.LookRotation(flat);
                body.rotation = Quaternion.RotateTowards(body.rotation, want, 540f * Time.deltaTime);
            }

            if (cooldown <= 0f && Quaternion.Angle(body.rotation, Quaternion.LookRotation(flat)) < 12f)
            {
                // Re-solve from the muzzle's position after turning so the shot is exact
                Ballistics.TryLeadTarget(muzzle.position, target.AimPoint, target.Velocity,
                                         Stats.ProjectileSpeed, Stats.HighArc, out launchVelocity, out _);
                Shell.Create(muzzle.position, launchVelocity, Stats);
                Visuals.Burst(muzzle.position, Stats.SplashRadius > 0f ? 0.7f : 0.35f, Stats.ShotColor, 0.1f, false);
                cooldown += 1f / Stats.FireRate;
            }
        }

        private bool InRange(Enemy e)
        {
            Vector3 d = e.transform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= Stats.Range * Stats.Range;
        }

        /// <summary>Picks the enemy closest to the base (furthest along the route) within range.</summary>
        private Enemy FindTarget(GameManager gm)
        {
            Enemy best = null;
            foreach (Enemy e in gm.Enemies)
            {
                if (e == null || e.IsDead || !InRange(e)) continue;
                if (best == null || e.Distance > best.Distance) best = e;
            }
            return best;
        }
    }
}
