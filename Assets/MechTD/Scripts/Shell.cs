using System.Collections.Generic;
using UnityEngine;

namespace MechTD
{
    /// <summary>
    /// A physics projectile: a Rigidbody with gravity launched by a tower.
    /// Damages the enemy it hits, or everything in its splash radius.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Shell : MonoBehaviour
    {
        public const int Layer = 2; // built-in "Ignore Raycast" layer; shells don't collide with each other

        private Rigidbody rb;
        private float damage, splashRadius, life = 10f;
        private Color color;
        private bool exploded;

        public static Shell Create(Vector3 position, Vector3 velocity, TowerStats stats)
        {
            float size = stats.SplashRadius > 0f ? 0.55f : (stats.Kind == TowerKind.Longshot ? 0.32f : 0.26f);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = stats.Name + " Shell";
            go.layer = Layer;
            go.transform.position = position;
            go.transform.localScale = new Vector3(size, size, size * (stats.SplashRadius > 0f ? 1.3f : 2.2f));
            go.GetComponent<Renderer>().sharedMaterial = Visuals.Lit(stats.ShotColor, 3f);

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = stats.Kind == TowerKind.Longshot ? 0.25f : 0.15f;
            trail.startWidth = size * 0.9f;
            trail.endWidth = 0f;
            trail.sharedMaterial = Visuals.Unlit(Color.white);
            trail.startColor = stats.ShotColor;
            trail.endColor = new Color(stats.ShotColor.r, stats.ShotColor.g, stats.ShotColor.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = velocity;

            var shell = go.AddComponent<Shell>();
            shell.rb = rb;
            shell.damage = stats.Damage;
            shell.splashRadius = stats.SplashRadius;
            shell.color = stats.ShotColor;
            go.transform.rotation = Quaternion.LookRotation(velocity);
            return shell;
        }

        void FixedUpdate()
        {
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
                rb.MoveRotation(Quaternion.LookRotation(rb.linearVelocity));
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f || transform.position.y < -10f) Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (exploded) return;
            exploded = true;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            if (splashRadius > 0f)
            {
                var hit = new HashSet<Enemy>();
                foreach (Collider c in Physics.OverlapSphere(point, splashRadius))
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && hit.Add(e))
                    {
                        // Full damage at the center, half at the edge
                        float d = Vector3.Distance(point, c.ClosestPoint(point));
                        e.TakeDamage(damage * Mathf.Lerp(1f, 0.5f, d / splashRadius));
                    }
                }
                Visuals.Burst(point, splashRadius * 0.6f, color, 0.5f);
            }
            else
            {
                Enemy enemy = collision.collider.GetComponentInParent<Enemy>();
                if (enemy != null) enemy.TakeDamage(damage);
                Visuals.Burst(point, enemy != null ? 0.9f : 0.5f, color, 0.25f, enemy != null);
            }

            Destroy(gameObject);
        }
    }
}
