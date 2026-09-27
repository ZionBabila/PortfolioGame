using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.EditorTools
{
    /// <summary>Helpers for swapping the placeholder primitives for real low-poly art.</summary>
    public static class PortfolioArtTools
    {
        const string ConvertedDir = "Assets/_Portfolio/Materials/Converted";
        static readonly Color Ink = new(0.08f, 0.07f, 0.1f);

        /// <summary>
        /// Gives every renderer under the selection a Portfolio/ToonOutline material that keeps the
        /// original color and texture. Converted materials are shared: the same source material maps
        /// to the same toon material, so converting a whole pack keeps it consistent.
        /// </summary>
        [MenuItem("Portfolio/Art/Convert Selection To Toon")]
        static void ConvertSelection()
        {
            var shader = Shader.Find("Portfolio/ToonOutline");
            EnsureFolder(ConvertedDir);
            int count = 0;

            foreach (var go in Selection.gameObjects)
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                var smooth = HasSmoothedNormals(r);
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (!src || src.shader == shader) continue;
                    mats[i] = ToonFor(src, shader, smooth);
                    count++;
                }
                Undo.RecordObject(r, "Convert To Toon");
                r.sharedMaterials = mats;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Portfolio] Converted {count} material slot(s) to toon. Tune outline width per material in {ConvertedDir}.");
        }

        [MenuItem("Portfolio/Art/Convert Selection To Toon", true)]
        static bool ConvertSelectionValidate() => Selection.gameObjects.Length > 0;

        internal static bool HasSmoothedNormals(Renderer r)
        {
            var mf = r.GetComponent<MeshFilter>();
            return mf && mf.sharedMesh && mf.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord3);
        }

        internal static Material ToonFor(Material src, Shader shader, bool smoothedNormals, float outlineWidth = 0.04f)
        {
            EnsureFolder(ConvertedDir);
            var path = $"{ConvertedDir}/{Sanitize(src.name)}_Toon.mat";
            var color = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                      : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            var tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap")
                    : src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat)
            {
                // Existing toon materials keep your tweaks; only pick up a texture added in Blender since.
                if (tex && !mat.GetTexture("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetColor("_BaseColor", Color.white);
                    EditorUtility.SetDirty(mat);
                }
                return mat;
            }

            mat = new Material(shader);
            mat.SetColor("_BaseColor", tex ? Color.white : color); // a texture carries the color itself
            mat.SetColor("_ShadeColor", Color.Lerp(Color.white, new Color(0.35f, 0.3f, 0.5f), 0.55f));
            if (tex) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_OutlineColor", Ink);
            mat.SetFloat("_OutlineWidth", outlineWidth);
            // Models under Art/Models get smoothed normals baked into UV3 on import (closed outline hull).
            // Anything else falls back to raw normals, since imported meshes are rarely convex around their pivot.
            mat.SetFloat("_OutlineSource", smoothedNormals ? 2f : 0f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>Resizes a station's click collider to wrap whatever is under its Visual child.</summary>
        [MenuItem("Portfolio/Art/Fit Station Click Area")]
        static void FitStationColliders()
        {
            int count = 0;
            foreach (var go in Selection.gameObjects)
            {
                var station = go.GetComponentInParent<Station>();
                if (!station) continue;
                var box = station.GetComponent<BoxCollider>();
                var renderers = station.GetComponentsInChildren<Renderer>();
                if (!box || renderers.Length == 0) continue;

                var bounds = new Bounds();
                bool first = true;
                foreach (var r in renderers)
                {
                    if (r is not MeshRenderer || r.GetComponent<TMPro.TMP_Text>()) continue; // skip the label
                    if (first) { bounds = r.bounds; first = false; }
                    else bounds.Encapsulate(r.bounds);
                }
                if (first) continue;

                Undo.RecordObject(box, "Fit Station Click Area");
                var t = station.transform;
                box.center = t.InverseTransformPoint(bounds.center);
                box.size = t.InverseTransformVector(bounds.size);
                box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
                count++;
            }
            Debug.Log($"[Portfolio] Fitted {count} station click area(s).");
        }

        /// <summary>Re-bakes the walkable area after moving or adding art. Only colliders under "World" count.</summary>
        [MenuItem("Portfolio/Bake NavMesh")]
        static void BakeNavMesh()
        {
            var surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (!surface) { Debug.LogError("[Portfolio] No NavMeshSurface in the open scene."); return; }

            surface.BuildNavMesh();
            var scenePath = surface.gameObject.scene.path;
            var navPath = Path.Combine(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath) + "_NavMesh.asset").Replace('\\', '/');
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
            EditorSceneManager.SaveScene(surface.gameObject.scene);
            Debug.Log($"[Portfolio] NavMesh baked -> {navPath}");
        }

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
