using System.Collections.Generic;
using UnityEngine;

namespace MechTD
{
    /// <summary>
    /// Builds every visual in the game: materials, imported Kenney models (CC0), the
    /// procedurally assembled mechs, health bars and effects.
    /// </summary>
    public static class Visuals
    {
        private static readonly Dictionary<string, Material> litCache = new Dictionary<string, Material>();
        private static Shader litShader, unlitShader;

        // ---------- Materials ----------

        public static Material Lit(Color color, float emission = 0f, float metallic = 0.35f, float smoothness = 0.55f)
        {
            string key = $"{color}{emission}{metallic}{smoothness}";
            if (litCache.TryGetValue(key, out Material cached) && cached != null) return cached;

            if (litShader == null)
                litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(litShader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", color * emission);
            }
            litCache[key] = mat;
            return mat;
        }

        /// <summary>Unlit, vertex-colored, alpha-blended material (works in URP and built-in).</summary>
        public static Material Unlit(Color color)
        {
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            return new Material(unlitShader) { color = color };
        }

        // ---------- Primitive helpers ----------

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 size,
                                      Material mat, Vector3? euler = null, string name = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name ?? type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static Transform Box(Transform parent, Vector3 pos, Vector3 size, Material mat, string name = "Box")
            => Prim(PrimitiveType.Cube, parent, pos, size, mat, null, name).transform;

        public static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        // ---------- Imported models ----------

        /// <summary>
        /// Loads a model from Resources/MechTD/Models, scales it so its height (or footprint) equals
        /// <paramref name="size"/>, and puts its base center at the local origin. Returns null if missing.
        /// </summary>
        public static GameObject LoadModel(string modelName, float size, bool fitHeight, float yawOffset = 0f)
        {
            var prefab = Resources.Load<GameObject>("MechTD/Models/" + modelName);
            if (prefab == null) return null;

            var holder = new GameObject(modelName);
            GameObject model = Object.Instantiate(prefab, holder.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, yawOffset, 0f);
            foreach (Collider c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            Bounds b = RendererBounds(model);
            if (b.size == Vector3.zero) return holder;
            float current = fitHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (current > 0.0001f) model.transform.localScale *= size / current;

            b = RendererBounds(model);
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z) - holder.transform.position;
            return holder;
        }

        public static Bounds RendererBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // ---------- Mechs ----------

        public class MechParts
        {
            public Transform Root;       // whole mech
            public Transform Body;       // yaw pivot (rotates to aim)
            public Transform Muzzle;     // where shells leave the barrel
            public Transform LeftLeg, RightLeg;
        }

        /// <summary>
        /// Assembles a humanoid mech from primitives. Built 2 units tall at scale 1 and then
        /// scaled to <paramref name="height"/>. Player and enemy mechs share the skeleton but
        /// use different head, armor and weapon styles.
        /// </summary>
        public static MechParts BuildMech(string name, float height, Color primary, Color secondary,
                                          Color accent, Color visor, bool enemy, float bulk = 1f, float legScale = 1f)
        {
            var parts = new MechParts();
            var root = new GameObject(name).transform;
            parts.Root = root;

            Material mPrim = Lit(primary, 0f, 0.3f, 0.6f);
            Material mSec = Lit(secondary, 0f, 0.5f, 0.5f);
            Material mAcc = Lit(accent, 0.6f, 0.2f, 0.6f);
            Material mJoint = Lit(new Color(0.18f, 0.18f, 0.2f), 0f, 0.7f, 0.4f);
            Material mVisor = Lit(visor, 4f, 0f, 0.9f);

            Transform body = Pivot(root, "Body", Vector3.zero);
            parts.Body = body;
            float hip = 1.0f * legScale;
            float w = bulk;

            // Legs (pivot at hip so they can swing when walking)
            for (int side = -1; side <= 1; side += 2)
            {
                Transform leg = Pivot(body, side < 0 ? "LeftLeg" : "RightLeg", new Vector3(0.22f * side * w, hip, 0f));
                Box(leg, new Vector3(0, -0.25f * legScale, 0), new Vector3(0.26f * w, 0.5f * legScale, 0.3f), mJoint, "Thigh");
                Box(leg, new Vector3(0, -0.72f * legScale, 0.02f), new Vector3(0.32f * w, 0.5f * legScale, 0.36f), mPrim, "Shin");
                Box(leg, new Vector3(0, -0.5f * legScale, 0.18f), new Vector3(0.22f * w, 0.18f, 0.12f), mSec, "Knee");
                Box(leg, new Vector3(0, -0.94f * legScale, 0.07f), new Vector3(0.34f * w, 0.12f, 0.52f), mSec, "Foot");
                if (side < 0) parts.LeftLeg = leg; else parts.RightLeg = leg;
            }

            // Torso
            Box(body, new Vector3(0, hip + 0.06f, 0), new Vector3(0.52f * w, 0.18f, 0.34f), mSec, "Waist");
            Box(body, new Vector3(0, hip + 0.18f, 0), new Vector3(0.4f * w, 0.2f, 0.3f), mJoint, "Abdomen");
            Box(body, new Vector3(0, hip + 0.42f, 0), new Vector3(0.8f * w, 0.48f, 0.48f), mPrim, "Chest");
            Box(body, new Vector3(0, hip + 0.42f, 0.22f), new Vector3(0.5f * w, 0.3f, 0.08f), mSec, "ChestPlate");
            for (int side = -1; side <= 1; side += 2)
                Box(body, new Vector3(0.17f * side * w, hip + 0.5f, 0.27f), new Vector3(0.14f, 0.1f, 0.04f), mAcc, "Vent");

            // Backpack + thrusters
            Box(body, new Vector3(0, hip + 0.45f, -0.34f), new Vector3(0.5f * w, 0.5f, 0.24f), mSec, "Backpack");
            for (int side = -1; side <= 1; side += 2)
                Prim(PrimitiveType.Cylinder, body, new Vector3(0.15f * side * w, hip + 0.2f, -0.4f),
                     new Vector3(0.14f, 0.12f, 0.14f), mJoint, null, "Thruster");

            // Head
            if (enemy)
            {
                Box(body, new Vector3(0, hip + 0.8f, 0.02f), new Vector3(0.34f, 0.2f, 0.32f), mPrim, "Head");
                Box(body, new Vector3(0, hip + 0.8f, 0.17f), new Vector3(0.3f, 0.05f, 0.03f), mVisor, "VisorSlit");
                Box(body, new Vector3(0, hip + 0.93f, -0.02f), new Vector3(0.06f, 0.08f, 0.3f), mSec, "Crest");
            }
            else
            {
                Box(body, new Vector3(0, hip + 0.8f, 0.02f), new Vector3(0.24f, 0.24f, 0.26f), mPrim, "Head");
                Box(body, new Vector3(0, hip + 0.82f, 0.15f), new Vector3(0.2f, 0.06f, 0.03f), mVisor, "Visor");
                Box(body, new Vector3(0, hip + 0.71f, 0.15f), new Vector3(0.1f, 0.06f, 0.04f), mAcc, "Chin");
                for (int side = -1; side <= 1; side += 2)
                    Box(body, new Vector3(0.09f * side, hip + 0.98f, -0.02f), new Vector3(0.03f, 0.18f, 0.03f), mAcc, "Antenna");
            }

            // Shoulders and arms
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.56f * side * w;
                Box(body, new Vector3(x, hip + 0.55f, 0), new Vector3(0.36f, enemy ? 0.34f : 0.28f, 0.42f) * (enemy ? 1.1f : 1f), mSec, "Shoulder");
                Box(body, new Vector3(x, hip + 0.27f, 0), new Vector3(0.18f, 0.36f, 0.2f), mJoint, "UpperArm");
                Box(body, new Vector3(x, hip - 0.02f, 0.08f), new Vector3(0.24f, 0.34f, 0.26f), mPrim, "Forearm");
            }

            root.localScale = Vector3.one * (height / 2f);
            return parts;
        }

        /// <summary>Adds a rifle-style weapon to the mech's right side and returns its muzzle.</summary>
        public static Transform AddRifle(MechParts mech, float length, Material body, Material accent, float hip = 1f)
        {
            Transform gun = Pivot(mech.Body, "Rifle", new Vector3(0.62f, hip + 0.02f, 0.25f));
            Box(gun, new Vector3(0, 0, 0.1f), new Vector3(0.14f, 0.2f, 0.7f), body, "Receiver");
            Box(gun, new Vector3(0, -0.14f, -0.05f), new Vector3(0.08f, 0.16f, 0.1f), body, "Grip");
            Box(gun, new Vector3(0, 0.15f, 0.05f), new Vector3(0.08f, 0.08f, 0.3f), accent, "Scope");
            Prim(PrimitiveType.Cylinder, gun, new Vector3(0, 0.02f, 0.45f + length * 0.5f),
                 new Vector3(0.07f, length * 0.5f, 0.07f), body, new Vector3(90, 0, 0), "Barrel");
            return Pivot(gun, "Muzzle", new Vector3(0, 0.02f, 0.5f + length));
        }

        // ---------- Health bar ----------

        public static HealthBar AddHealthBar(Transform parent, float worldHeight, float width)
        {
            var holder = new GameObject("HealthBar").transform;
            holder.SetParent(parent, false);
            holder.position = parent.position + Vector3.up * worldHeight;
            var bg = Prim(PrimitiveType.Quad, holder, Vector3.zero, new Vector3(width + 0.12f, 0.36f, 1f), Unlit(new Color(0, 0, 0, 0.75f)));
            var fill = Prim(PrimitiveType.Quad, holder, new Vector3(0, 0, -0.01f), new Vector3(width, 0.24f, 1f), Unlit(Color.green));
            var bar = holder.gameObject.AddComponent<HealthBar>();
            bar.Init(fill.transform, fill.GetComponent<Renderer>(), width);
            bg.name = "Background";
            fill.name = "Fill";
            return bar;
        }

        // ---------- Effects ----------

        public static void Burst(Vector3 position, float radius, Color color, float duration = 0.35f, bool light = true)
        {
            var go = Prim(PrimitiveType.Sphere, null, position, Vector3.one * 0.1f, Unlit(color), null, "Burst");
            go.transform.position = position;
            var fx = go.AddComponent<FxBurst>();
            fx.Init(radius, duration, color, light);
        }
    }
}
