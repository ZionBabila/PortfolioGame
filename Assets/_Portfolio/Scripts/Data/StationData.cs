using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public enum StationKind { About, Project, Game, Career, Links }

    [Serializable]
    public class StationLink
    {
        public string label;
        [Tooltip("Absolute URL, or a path relative to the site root (e.g. games/my-game/) for builds hosted next to the portfolio.")]
        public string url;
    }

    /// <summary>All content of a single station on the map. Edit these assets to update the portfolio — no code changes needed.</summary>
    [CreateAssetMenu(menuName = "Portfolio/Station", fileName = "Station")]
    public class StationData : ScriptableObject
    {
        public string title;
        public StationKind kind;
        public string subtitle;
        [TextArea(4, 14)] public string body;
        public Sprite image;
        public Color accent = new(0.95f, 0.55f, 0.3f);
        public List<StationLink> links = new();
        [Tooltip("Project cards shown above the links; each opens a full-screen project page.")]
        public List<ProjectData> projects = new();
    }
}
