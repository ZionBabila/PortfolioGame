using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Cinemachine setup for the Workshop:
    ///  * "CM Follow" follows the player; CinemachineRoomConfiner keeps the view inside the walls;
    ///  * one CameraZone per station: when the player walks up to it, the brain blends to a closer shot;
    ///  * Portfolio → Camera → Add Camera Zone creates extra zones to place by hand.
    /// </summary>
    public static partial class PortfolioSceneBuilder
    {
        const int FollowPriority = 10;
        const float CameraDistance = 30f;

        struct RoomLimits
        {
            public Vector2 RoomMin, RoomMax, ViewMin, ViewMax;
            public float FloorY, WallHeight;
        }

        static RoomLimits roomLimits;

        static void BuildCinemachineRig(Transform player, List<Station> stations, Quaternion viewRotation,
            Bounds floor, Bounds walls, Bounds view)
        {
            roomLimits = new RoomLimits
            {
                RoomMin = new Vector2(floor.min.x, floor.min.z),
                RoomMax = new Vector2(floor.max.x, floor.max.z),
                ViewMin = new Vector2(view.min.x, view.min.z),
                ViewMax = new Vector2(view.max.x, view.max.z),
                FloorY = floor.max.y,
                WallHeight = walls.max.y - floor.max.y,
            };
            var rigRoot = new GameObject("Cameras").transform;

            var follow = NewVcam("CM Follow", rigRoot, viewRotation, player, FollowPriority);
            var followComposer = follow.GetComponent<CinemachinePositionComposer>();
            followComposer.Damping = new Vector3(0.6f, 0.6f, 0.6f);
            var comp = followComposer.Composition;
            comp.DeadZone.Enabled = true;
            comp.DeadZone.Size = new Vector2(0.15f, 0.15f);
            followComposer.Composition = comp;
            SetValues(follow.GetComponent<AspectLens>(), ("landscapeSize", 4.2f), ("portraitVisibleWidth", 6.5f));

            var zonesRoot = new GameObject("Camera Zones").transform;
            foreach (var station in stations)
            {
                var title = station.data ? station.data.title : station.name;
                var vcam = NewVcam("CM Zone " + title, rigRoot, viewRotation, station.transform, 0);
                vcam.GetComponent<CinemachinePositionComposer>().TargetOffset = new Vector3(0f, 0.8f, 0f);
                // Keep the station clear of the content panel: it docks right in landscape, bottom (58%) in portrait.
                SetValues(vcam.GetComponent<AspectLens>(),
                    ("landscapeSize", 3f), ("landscapeScreenPosition", new Vector2(-0.18f, 0f)),
                    ("portraitVisibleWidth", 5f), ("portraitScreenPosition", new Vector2(0f, -0.27f))); // +Y is down in CM3

                var zone = NewZone("Zone " + title, zonesRoot, vcam);
                zone.transform.SetPositionAndRotation(station.ApproachPosition, station.transform.rotation);
                var box = zone.GetComponent<BoxCollider>();
                box.center = new Vector3(0f, 1.2f, 0.4f);
                box.size = new Vector3(3.6f, 2.4f, 3.2f);
            }
        }

        static CinemachineCamera NewVcam(string name, Transform parent, Quaternion rotation, Transform target, int priority)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var pos = target ? target.position : Vector3.zero;
            go.transform.SetPositionAndRotation(pos - rotation * Vector3.forward * CameraDistance, rotation);

            var vcam = go.AddComponent<CinemachineCamera>();
            var lens = vcam.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = 4.2f;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 200f;
            vcam.Lens = lens;
            vcam.Priority = priority;
            vcam.Follow = target;

            var composer = go.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = CameraDistance;
            composer.Damping = new Vector3(0.4f, 0.4f, 0.4f);

            var confiner = go.AddComponent<CinemachineRoomConfiner>();
            confiner.RoomMin = roomLimits.RoomMin;
            confiner.RoomMax = roomLimits.RoomMax;
            confiner.ViewFloorMin = roomLimits.ViewMin;
            confiner.ViewFloorMax = roomLimits.ViewMax;
            confiner.FloorY = roomLimits.FloorY;
            confiner.WallHeight = roomLimits.WallHeight;

            go.AddComponent<AspectLens>();
            return vcam;
        }

        static CameraZone NewZone(string name, Transform parent, CinemachineCamera vcam)
        {
            var go = new GameObject(name) { layer = 2 }; // Ignore Raycast: never blocks floor clicks
            go.transform.SetParent(parent, false);
            go.AddComponent<BoxCollider>().isTrigger = true;
            var zone = go.AddComponent<CameraZone>();
            SetRefs(zone, ("zoneCamera", vcam));
            return zone;
        }

        /// <summary>
        /// Adds a hand-placed zone at the Scene view pivot: a trigger box plus a camera that copies the
        /// follow camera's angle and room limits and frames a "Zone Target". Move the box to where the
        /// player should trigger it, and the target (or the camera's lens) to frame the shot.
        /// </summary>
        [MenuItem("Portfolio/Camera/Add Camera Zone")]
        static void AddCameraZone()
        {
            var followGo = GameObject.Find("CM Follow");
            var followConfiner = followGo ? followGo.GetComponent<CinemachineRoomConfiner>() : null;
            if (!followConfiner)
            {
                EditorUtility.DisplayDialog("Add Camera Zone", "Open the Workshop scene first (no \"CM Follow\" camera found).", "OK");
                return;
            }
            roomLimits = new RoomLimits
            {
                RoomMin = followConfiner.RoomMin, RoomMax = followConfiner.RoomMax,
                ViewMin = followConfiner.ViewFloorMin, ViewMax = followConfiner.ViewFloorMax,
                FloorY = followConfiner.FloorY, WallHeight = followConfiner.WallHeight,
            };

            var pivot = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            pivot.y = followConfiner.FloorY;
            var zonesGo = GameObject.Find("Camera Zones");
            var zonesRoot = zonesGo ? zonesGo.transform : new GameObject("Camera Zones").transform;
            int n = zonesRoot.childCount + 1;

            var target = new GameObject($"Zone Target {n}").transform;
            target.SetParent(zonesRoot, false);
            target.position = pivot;

            var vcam = NewVcam($"CM Zone Custom {n}", followGo.transform.parent, followGo.transform.rotation, target, 0);
            SetValues(vcam.GetComponent<AspectLens>(), ("landscapeSize", 3.5f), ("portraitVisibleWidth", 5.5f));
            var zone = NewZone($"Zone Custom {n}", zonesRoot, vcam);
            zone.transform.position = pivot;
            var box = zone.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.2f, 0f);
            box.size = new Vector3(4f, 2.4f, 4f);

            Undo.RegisterCreatedObjectUndo(target.gameObject, "Add Camera Zone");
            Undo.RegisterCreatedObjectUndo(vcam.gameObject, "Add Camera Zone");
            Undo.RegisterCreatedObjectUndo(zone.gameObject, "Add Camera Zone");
            Selection.activeGameObject = zone.gameObject;
        }
    }
}
