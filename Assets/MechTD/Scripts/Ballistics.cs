using UnityEngine;

namespace MechTD
{
    /// <summary>
    /// Projectile math. Shells are real Rigidbodies affected by gravity, so towers
    /// solve for the launch velocity that makes the arc pass through the aim point.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>
        /// Launch velocity with fixed speed <paramref name="speed"/> that hits a point at
        /// <paramref name="displacement"/> from the muzzle under gravity <paramref name="g"/>.
        /// Returns false if the point is out of range at that speed.
        /// </summary>
        public static bool TrySolveLaunch(Vector3 displacement, float speed, float g, bool highArc,
                                          out Vector3 launchVelocity, out float flightTime)
        {
            launchVelocity = Vector3.zero;
            flightTime = 0f;

            // |d - 0.5 g t^2|^2 = (v t)^2  ->  quadratic in T = t^2:
            // (g^2/4) T^2 - (v^2 - g*dy) T + |d|^2 = 0
            float a = 0.25f * g * g;
            float b = -(speed * speed - g * displacement.y);
            float c = displacement.sqrMagnitude;
            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;

            float sqrtDisc = Mathf.Sqrt(disc);
            float t2 = highArc ? (-b + sqrtDisc) / (2f * a) : (-b - sqrtDisc) / (2f * a);
            if (t2 <= 0f) return false;

            float t = Mathf.Sqrt(t2);
            launchVelocity = new Vector3(
                displacement.x / t,
                displacement.y / t + 0.5f * g * t,
                displacement.z / t);
            flightTime = t;
            return true;
        }

        /// <summary>
        /// Predictive aim: find where the target will be when the shell arrives, assuming it
        /// keeps its current velocity, and solve the launch toward that point. Iterates
        /// because the flight time depends on the aim point and vice versa.
        /// </summary>
        public static bool TryLeadTarget(Vector3 muzzle, Vector3 targetPos, Vector3 targetVel,
                                         float speed, bool highArc,
                                         out Vector3 launchVelocity, out Vector3 aimPoint)
        {
            float g = -Physics.gravity.y;
            aimPoint = targetPos;
            launchVelocity = Vector3.zero;

            for (int i = 0; i < 8; i++)
            {
                if (!TrySolveLaunch(aimPoint - muzzle, speed, g, highArc, out launchVelocity, out float t))
                    return false;

                // Unity integrates gravity with semi-implicit Euler, which drops the shell an
                // extra 0.5*g*dt*t compared to the exact parabola. Aim that much higher.
                Vector3 next = targetPos + targetVel * t + Vector3.up * (0.5f * g * Time.fixedDeltaTime * t);
                bool converged = (next - aimPoint).sqrMagnitude < 0.0025f;
                aimPoint = next;
                if (converged) break;
            }

            return TrySolveLaunch(aimPoint - muzzle, speed, g, highArc, out launchVelocity, out _);
        }
    }
}
