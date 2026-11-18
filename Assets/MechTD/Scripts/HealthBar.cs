using UnityEngine;

namespace MechTD
{
    /// <summary>World-space health bar that always faces the camera.</summary>
    public class HealthBar : MonoBehaviour
    {
        private Transform fill;
        private Renderer fillRenderer;
        private float width;
        private Camera cam;

        public void Init(Transform fillTransform, Renderer renderer, float barWidth)
        {
            fill = fillTransform;
            fillRenderer = renderer;
            width = barWidth;
        }

        public void Set(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            fill.localScale = new Vector3(width * fraction, fill.localScale.y, 1f);
            fill.localPosition = new Vector3(-(1f - fraction) * width * 0.5f, 0f, -0.01f);
            fillRenderer.material.color = Color.Lerp(new Color(1f, 0.2f, 0.15f), new Color(0.3f, 1f, 0.35f), fraction);
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
