using UnityEngine;

namespace MechTD
{
    /// <summary>A predetermined, non-straight route made of waypoints that enemies follow.</summary>
    public class EnemyPath
    {
        public readonly Vector3[] Points;
        public readonly float Length;
        public const float HalfWidth = 2.6f;

        private readonly float[] cumulative;

        public EnemyPath(Vector3[] points)
        {
            Points = points;
            cumulative = new float[points.Length];
            for (int i = 1; i < points.Length; i++)
                cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            Length = cumulative[points.Length - 1];
        }

        /// <summary>Position and travel direction at <paramref name="distance"/> meters along the route.</summary>
        public Vector3 Evaluate(float distance, out Vector3 direction)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            for (int i = 1; i < Points.Length; i++)
            {
                if (distance <= cumulative[i] || i == Points.Length - 1)
                {
                    float segLen = cumulative[i] - cumulative[i - 1];
                    float t = segLen > 0f ? (distance - cumulative[i - 1]) / segLen : 0f;
                    direction = (Points[i] - Points[i - 1]).normalized;
                    return Vector3.Lerp(Points[i - 1], Points[i], t);
                }
            }
            direction = Vector3.forward;
            return Points[0];
        }

        /// <summary>Horizontal distance from a point to the closest part of the route.</summary>
        public float DistanceTo(Vector3 p)
        {
            p.y = 0f;
            float best = float.MaxValue;
            for (int i = 1; i < Points.Length; i++)
            {
                Vector3 a = Points[i - 1]; a.y = 0f;
                Vector3 b = Points[i]; b.y = 0f;
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector3.Distance(p, a + ab * t));
            }
            return best;
        }
    }
}
