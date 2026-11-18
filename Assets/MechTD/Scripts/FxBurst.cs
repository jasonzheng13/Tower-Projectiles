using UnityEngine;

namespace MechTD
{
    /// <summary>Short-lived expanding glow sphere with a light flash (explosions, muzzle flashes).</summary>
    public class FxBurst : MonoBehaviour
    {
        private float radius, duration, age;
        private Material mat;
        private Color color;
        private Light flash;

        public void Init(float burstRadius, float burstDuration, Color burstColor, bool withLight)
        {
            radius = burstRadius;
            duration = burstDuration;
            color = burstColor;
            mat = GetComponent<Renderer>().material;
            if (withLight)
            {
                flash = gameObject.AddComponent<Light>();
                flash.type = LightType.Point;
                flash.color = burstColor;
                flash.range = burstRadius * 4f;
                flash.intensity = 6f;
            }
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            transform.localScale = Vector3.one * (radius * 2f * Mathf.Sqrt(t) + 0.05f);
            Color c = Color.Lerp(Color.white, color, t * 2f);
            c.a = 1f - t;
            mat.color = c;
            if (flash != null) flash.intensity = 6f * (1f - t);
            if (age >= duration) Destroy(gameObject);
        }
    }
}
