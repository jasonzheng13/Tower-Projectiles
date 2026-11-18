using UnityEngine;

namespace MechTD
{
    /// <summary>Procedural walk cycle: swings the leg pivots and bobs the body.</summary>
    public class MechWalker : MonoBehaviour
    {
        public Transform LeftLeg, RightLeg, Body;
        public float StepsPerSecond = 1.5f;
        public float SwingDegrees = 28f;
        private float phase;
        private Vector3 bodyRest;

        void Start()
        {
            if (Body != null) bodyRest = Body.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            phase += Time.deltaTime * StepsPerSecond * Mathf.PI;
            float s = Mathf.Sin(phase);
            if (LeftLeg != null) LeftLeg.localRotation = Quaternion.Euler(s * SwingDegrees, 0, 0);
            if (RightLeg != null) RightLeg.localRotation = Quaternion.Euler(-s * SwingDegrees, 0, 0);
            if (Body != null) Body.localPosition = bodyRest + Vector3.up * Mathf.Abs(Mathf.Cos(phase)) * 0.05f;
        }
    }
}
