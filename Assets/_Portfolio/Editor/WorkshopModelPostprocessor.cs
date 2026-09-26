using System.Collections.Generic;
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

        void OnPostprocessModel(GameObject root)
        {
            if (!InScope) return;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh) BakeSmoothedNormals(mf.sharedMesh);
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
