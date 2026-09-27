using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AssetImporters;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Import settings for everything exported from Blender into Assets/_Portfolio/Art/Models:
    ///  * converts Blender's Z-up axes in the vertex data so transforms come in clean;
    ///  * bakes a smoothed normal per vertex into UV3 so the ToonOutline inverted hull stays closed
    ///    on hard-edged low-poly meshes (_OutlineSource = SmoothedNormals).
    /// </summary>
    public class WorkshopModelPostprocessor : AssetPostprocessor
    {
        const string ModelsDir = "Assets/_Portfolio/Art/Models/";

        bool InScope => assetPath.StartsWith(ModelsDir);

        void OnPreprocessModel()
        {
            if (!InScope) return;
            var importer = (ModelImporter)assetImporter;
            importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importNormals = ModelImporterNormals.Import;
        }

        // Run after URP's own material setup, so the texture fix below has the last word.
        public override int GetPostprocessOrder() => 100;

        /// <summary>
        /// Blender stores image paths relative to the .blend, which break once Unity converts the file in a temp
        /// folder, so imported materials arrive without their texture. Find it in the project by file name.
        /// </summary>
        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (!InScope) return;
            string file = null, preferFolder = null;
            if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription tex))
            {
                if (tex.texture) { material.SetTexture("_BaseMap", tex.texture); return; }
                // The path arrives mangled (e.g. "C//Users/..."), so only trust the file name.
                var raw = !string.IsNullOrEmpty(tex.relativePath) ? tex.relativePath : tex.path;
                file = Path.GetFileNameWithoutExtension((raw ?? "").Replace('\\', '/'));
            }
            else if (TryPoliigonBaseColor(material.name, out var poliigonFile, out var size))
            {
                // Poliigon's add-on routes the color through a "COLOR * AO" mix node, so no texture reaches the FBX.
                // Its names are predictable: Poliigon_<Name>_<Id>_2K → Poliigon_<Name>_<Id>_BaseColor in a 2K folder.
                file = poliigonFile;
                preferFolder = "/" + size + "/";
            }
            if (string.IsNullOrEmpty(file)) return;
            var found = AssetDatabase.FindAssets(file + " t:Texture2D")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == file)
                .OrderByDescending(p => preferFolder != null && p.Contains(preferFolder))
                .FirstOrDefault();
            if (found == null)
            {
                Debug.LogWarning($"[Portfolio] {assetPath}: texture '{file}' for material '{material.name}' isn't in the project. Put it under Assets/_Portfolio/Art/Textures.");
                return;
            }
            context.DependsOnSourceAsset(found);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(found));
            material.SetColor("_BaseColor", Color.white);
        }

        static bool TryPoliigonBaseColor(string materialName, out string baseColorFile, out string size)
        {
            baseColorFile = size = null;
            if (!materialName.StartsWith("Poliigon_")) return false;
            var m = System.Text.RegularExpressions.Regex.Match(materialName, @"^(Poliigon_.+?_\d+)(?:_(\d+K))?$");
            if (!m.Success) return false;
            baseColorFile = m.Groups[1].Value + "_BaseColor";
            size = m.Groups[2].Success ? m.Groups[2].Value : "";
            return true;
        }

        /// <summary>Textures for the models: 1024 is plenty for the toon look and keeps the web build small.</summary>
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Portfolio/Art/Textures/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = Mathf.Min(importer.maxTextureSize, 1024);
            importer.anisoLevel = 8; // keeps the grain sharp at the camera's slanted view
            // Only the base color is used by the toon shader; the other maps are data, not color.
            var name = Path.GetFileNameWithoutExtension(assetPath);
            if (!name.Contains("BaseColor") && !name.Contains("Albedo") && !name.Contains("Diffuse") && !name.Contains("Color"))
                importer.sRGBTexture = false;
            if (name.Contains("Normal")) importer.textureType = TextureImporterType.NormalMap;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!InScope) return;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh) BakeSmoothedNormals(mf.sharedMesh);
        }

        /// <summary>
        /// When Workshop.blend is saved in Blender (Unity re-imports it on focus), sync the open
        /// Workshop scene: new meshes get toon materials + colliders, new stations get set up, NavMesh re-bakes.
        /// Only adds what's missing; your scene changes are kept. Save the scene afterwards.
        /// </summary>
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Any(p => p.EndsWith("/Workshop.blend") || p.EndsWith("/Workshop.fbx"))) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var workshop = GameObject.Find("Workshop");
                if (!workshop || !PrefabUtility.IsPartOfPrefabInstance(workshop)) return; // scene not open: nothing to sync
                PortfolioSceneBuilder.SyncWorkshop();
            };
        }

        static void BakeSmoothedNormals(Mesh mesh)
        {
            var verts = mesh.vertices;
            var normals = mesh.normals;
            if (normals == null || normals.Length != verts.Length) return;

            // Average the normals of every vertex that shares a position (split by flat shading).
            var sums = new Dictionary<Vector3Int, Vector3>();
            static Vector3Int Key(Vector3 p) => new(
                Mathf.RoundToInt(p.x * 10000f), Mathf.RoundToInt(p.y * 10000f), Mathf.RoundToInt(p.z * 10000f));

            for (int i = 0; i < verts.Length; i++)
            {
                var k = Key(verts[i]);
                sums[k] = sums.TryGetValue(k, out var s) ? s + normals[i] : normals[i];
            }

            var smooth = new List<Vector3>(verts.Length);
            for (int i = 0; i < verts.Length; i++)
            {
                var n = sums[Key(verts[i])];
                smooth.Add(n.sqrMagnitude > 1e-8f ? n.normalized : normals[i]);
            }
            mesh.SetUVs(3, smooth);
        }
    }
}
