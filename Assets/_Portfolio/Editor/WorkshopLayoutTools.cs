using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Lets stations move in Blender without leaving their camera zones behind:
    /// Portfolio → Camera → Attach Zones To Stations parents each station's zone (and the point its camera follows)
    /// under the station, so moving ST_x in Blender carries them along. Also command-line entry points for doing it,
    /// and the Blender re-sync, with the editor closed.
    /// </summary>
    public static class WorkshopLayoutTools
    {
        const string ScenePath = "Assets/_Portfolio/Scenes/Workshop.unity";

        [MenuItem("Portfolio/Camera/Attach Zones To Stations")]
        public static void AttachZonesToStations()
        {
            int moved = 0;
            foreach (var zone in Object.FindObjectsByType<CameraZone>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(zone);
                var station = so.FindProperty("station").objectReferenceValue as Station;
                var vcam = so.FindProperty("zoneCamera").objectReferenceValue as CinemachineCamera;
                if (!station) continue;
                Attach(zone.transform, station.transform);
                if (vcam)
                {
                    if (vcam.Follow) Attach(vcam.Follow, station.transform);
                    if (vcam.LookAt && vcam.LookAt != vcam.Follow) Attach(vcam.LookAt, station.transform);
                }
                moved++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Portfolio] {moved} camera zone(s) now ride on their stations.");
        }

        static void Attach(Transform t, Transform station)
        {
            if (!t || t == station || t.IsChildOf(station)) return;
            Undo.SetTransformParent(t, station, "Attach Zone To Station");
            // keep the world pose: SetTransformParent keeps world position by default
        }

        // ---------- Command line (editor closed) ----------

        /// <summary>unity -batchmode -projectPath . -executeMethod Portfolio.EditorTools.WorkshopLayoutTools.BatchAttach -quit</summary>
        public static void BatchAttach() => InScene(AttachZonesToStations);

        /// <summary>After editing Workshop.blend: re-import, sync stations/colliders/NavMesh, save the scene.</summary>
        public static void BatchSync() => InScene(() =>
        {
            AssetDatabase.Refresh();
            PortfolioSceneBuilder.SyncWorkshop();
            SlotContentBuilder.Build();
            ToonColorSync.Sync();
        });

        static void InScene(System.Action work)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            work();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Portfolio] Workshop scene saved.");
        }
    }
}
