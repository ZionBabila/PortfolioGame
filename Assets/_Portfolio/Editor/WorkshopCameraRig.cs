using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Cinemachine setup for the Workshop:
    ///  * "CM Overview" shows as much of the room as fits, rising and zooming out as the player climbs;
    ///    CinemachineRoomConfiner keeps every view inside the walls;
    ///  * one CameraZone per station: when the player walks up to it, the brain blends to a closer shot;
    ///  * Portfolio → Camera → Add Camera Zone creates extra zones to place by hand.
    /// </summary>
    public static partial class PortfolioSceneBuilder
    {
        const int FollowPriority = 10;
        const float CameraDistance = 30f;
        // Overview framing before the confiner caps it to the widest view inside the walls.
        const float OverviewLandscapeSize = 4.0f; // max that fits at 16:9 is ~5.3; the gap is room for look-ahead
        const float OverviewPortraitWidth = 6.2f; // max at 9:16 is ~8.3 m across; the gap is room for look-ahead
        // How far the gallery camera turns from the main angle to face the mezzanine wall.
        const float GalleryYawOffset = 45f;

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

            // Overview: as much of the room as fits inside the walls, following the player with look-ahead
            // (rule of thirds toward where they face) and rising/zooming out as they climb. Zones zoom in from here.
            var overviewTarget = new GameObject("Overview Target").AddComponent<OverviewTarget>();
            overviewTarget.transform.SetParent(rigRoot, false);
            var roomCenter = new Vector3(floor.center.x, floor.max.y, floor.center.z);
            overviewTarget.transform.position = roomCenter;
            SetRefs(overviewTarget, ("player", player));
            SetValues(overviewTarget, ("roomCenter", roomCenter));

            var overview = NewVcam("CM Overview", rigRoot, viewRotation, overviewTarget.transform, FollowPriority);
            overview.GetComponent<CinemachinePositionComposer>().Damping = new Vector3(0.5f, 0.5f, 0.5f); // the target already smooths its look-ahead
            overview.GetComponent<CinemachineRoomConfiner>().PanRoom = 1f; // use the widest view that fits
            SetValues(overview.GetComponent<AspectLens>(),
                ("landscapeSize", OverviewLandscapeSize), ("portraitVisibleWidth", OverviewPortraitWidth),
                ("zoomOutPerMeter", 0.15f)); // +~0.5 on the stairs/mezzanine (3.2 m), up to the max that fits
            SetRefs(overview.GetComponent<AspectLens>(), ("heightSource", overviewTarget));
            SetRefs(overviewTarget, ("viewCamera", overview)); // look-ahead: two thirds of the frame ahead of the player

            var zonesRoot = new GameObject("Camera Zones").transform;

            // The mezzanine runs along the side wall, which the main angle sees edge-on. On the stairs and up
            // there, turn to face that wall so the whole gallery (and its station) reads clearly.
            var galleryRotation = Quaternion.Euler(40f, viewRotation.eulerAngles.y - GalleryYawOffset, 0f);
            var mezzanine = GameObject.Find("Mezzanine");
            if (mezzanine)
            {
                var gb = mezzanine.GetComponent<Renderer>().bounds;
                var galleryTarget = new GameObject("Gallery Target").transform;
                galleryTarget.SetParent(rigRoot, false);
                galleryTarget.position = new Vector3(gb.center.x, gb.max.y - 1.2f, gb.center.z);
                var galleryCam = NewVcam("CM Gallery", rigRoot, galleryRotation, galleryTarget, 0);
                SetValues(galleryCam.GetComponent<AspectLens>(), ("landscapeSize", 4.6f), ("portraitVisibleWidth", 7.5f));

                var zone = NewZone("Zone Gallery", zonesRoot, galleryCam);
                SetValues(zone, ("activePriority", 20));
                zone.transform.position = new Vector3(gb.center.x, floor.max.y, gb.center.z);
                var box = zone.GetComponent<BoxCollider>();
                // Pulled 0.6 m in from the room-facing sides, so the player standing nearby (or spawning in the
                // middle of the room) doesn't graze it and flip the camera to the gallery view.
                box.center = new Vector3(-0.3f, 3f, -0.3f);
                box.size = new Vector3(gb.size.x - 0.6f, 6f, gb.size.z - 0.6f);
            }

            foreach (var station in stations)
            {
                var title = station.data ? station.data.title : station.name;
                bool upstairs = station.ApproachPosition.y > floor.max.y + 1f;
                var vcam = NewVcam("CM Zone " + title, rigRoot, upstairs ? galleryRotation : viewRotation, station.transform, 0);
                vcam.GetComponent<CinemachinePositionComposer>().TargetOffset = new Vector3(0f, 0.8f, 0f);
                // Keep the station clear of the content panel: it docks right in landscape, bottom (58%) in portrait.
                SetValues(vcam.GetComponent<AspectLens>(),
                    ("landscapeSize", 3f), ("landscapeScreenPosition", new Vector2(-0.18f, 0f)),
                    ("portraitVisibleWidth", 5f), ("portraitScreenPosition", new Vector2(0f, -0.27f))); // +Y is down in CM3

                var zone = NewZone("Zone " + title, zonesRoot, vcam);
                SetValues(zone, ("activePriority", 25)); // wins over the gallery zone it sits inside
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
            var followGo = GameObject.Find("CM Overview");
            var followConfiner = followGo ? followGo.GetComponent<CinemachineRoomConfiner>() : null;
            if (!followConfiner)
            {
                EditorUtility.DisplayDialog("Add Camera Zone", "Open the Workshop scene first (no \"CM Overview\" camera found).", "OK");
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
