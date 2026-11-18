using UnityEngine;

namespace MechTD
{
    /// <summary>Spins an object around its local Y axis and optionally bobs it (drones, beacons).</summary>
    public class Spinner : MonoBehaviour
    {
        public float DegreesPerSecond = 90f;
        public float BobHeight = 0f;
        public float BobSpeed = 2f;
        private Vector3 rest;

        void Start() => rest = transform.localPosition;

        void Update()
        {
            transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, 0f, Space.Self);
            if (BobHeight > 0f)
                transform.localPosition = rest + Vector3.up * Mathf.Sin(Time.time * BobSpeed) * BobHeight;
        }
    }
}
