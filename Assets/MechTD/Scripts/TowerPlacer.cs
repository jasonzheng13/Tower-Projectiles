using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MechTD
{
    /// <summary>
    /// Player input for deploying mechs: pick a type (1/2/3 or the HUD cards), move the
    /// preview over open ground, left-click to deploy. Right-click or Esc cancels.
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        public TowerKind? Selected { get; private set; }
        public string Hint { get; private set; } = "";
        public bool HintIsError { get; private set; }

        private GameObject preview;
        private TowerKind previewKind;
        private Transform ring;
        private Renderer ringRenderer;
        private LineRenderer rangeLine;

        public void Select(TowerKind kind)
        {
            if (GameManager.Instance.State != GameState.Playing) return;
            Selected = kind;
        }

        public void Cancel() => Selected = null;

        void Update()
        {
            GameManager gm = GameManager.Instance;
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (gm.State != GameState.Playing) { Cancel(); UpdatePreview(false, Vector3.zero, false); return; }

            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Select(TowerKind.Striker);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Select(TowerKind.Longshot);
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Select(TowerKind.Bastion);
                if (kb.escapeKey.wasPressedThisFrame) Cancel();
            }
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) Cancel();

            if (Selected == null || mouse == null || Camera.main == null)
            {
                Hint = gm.TowersRemaining > 0 ? "" : "All mechs deployed - clear a wave to earn another";
                HintIsError = false;
                UpdatePreview(false, Vector3.zero, false);
                return;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (overUi || !new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
            {
                Hint = $"Move the cursor over open ground to place {TowerStats.Get(Selected.Value).Name}";
                HintIsError = false;
                UpdatePreview(false, Vector3.zero, false);
                return;
            }

            Vector3 point = ray.GetPoint(enter);
            point.y = 0f;
            bool ok = gm.CanPlaceAt(point, out string reason);
            UpdatePreview(true, point, ok);
            Hint = ok ? $"LEFT-CLICK to deploy {TowerStats.Get(Selected.Value).Name}   |   RIGHT-CLICK / ESC to cancel" : reason;
            HintIsError = !ok;

            if (ok && mouse.leftButton.wasPressedThisFrame)
            {
                gm.PlaceTower(Selected.Value, point);
                if (gm.TowersRemaining <= 0) Cancel();
            }
        }

        private void UpdatePreview(bool show, Vector3 position, bool valid)
        {
            if (!show || Selected == null)
            {
                if (preview != null) preview.SetActive(false);
                return;
            }

            if (preview == null || previewKind != Selected.Value) BuildPreview(Selected.Value);
            preview.SetActive(true);
            preview.transform.position = position;
            Color c = valid ? new Color(0.2f, 1f, 0.4f, 0.45f) : new Color(1f, 0.2f, 0.2f, 0.45f);
            ringRenderer.material.color = c;
            rangeLine.startColor = rangeLine.endColor = new Color(c.r, c.g, c.b, 0.9f);
        }

        private void BuildPreview(TowerKind kind)
        {
            if (preview != null) Destroy(preview);
            previewKind = kind;
            TowerStats stats = TowerStats.Get(kind);

            preview = new GameObject("Placement Preview");
            GameObject model = MechTower.BuildVisual(stats, out _, out _);
            model.transform.SetParent(preview.transform, false);

            ring = Visuals.Prim(PrimitiveType.Cylinder, preview.transform, new Vector3(0, 0.05f, 0),
                                new Vector3(3.2f, 0.02f, 3.2f), Visuals.Unlit(Color.green), null, "Footprint").transform;
            ringRenderer = ring.GetComponent<Renderer>();

            // Range circle
            var lineGo = new GameObject("Range");
            lineGo.transform.SetParent(preview.transform, false);
            rangeLine = lineGo.AddComponent<LineRenderer>();
            rangeLine.useWorldSpace = false;
            rangeLine.loop = true;
            rangeLine.widthMultiplier = 0.2f;
            rangeLine.sharedMaterial = Visuals.Unlit(Color.white);
            const int segments = 72;
            rangeLine.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                rangeLine.SetPosition(i, new Vector3(Mathf.Cos(a) * stats.Range, 0.15f, Mathf.Sin(a) * stats.Range));
            }
        }
    }
}
