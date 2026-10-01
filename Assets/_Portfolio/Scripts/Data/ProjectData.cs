using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    [Serializable]
    public class ProjectImage
    {
        public Sprite image;
        [Tooltip("One short line under the image, e.g. \"Foam models, round 2\". Optional.")]
        public string caption;
    }

    [Serializable]
    public class ProjectFact
    {
        [Tooltip("e.g. Materials, Manufacturing, Dimensions, Weight")]
        public string label;
        public string value;
    }

    /// <summary>
    /// One industrial-design project. Listed as a card inside a station (StationData.projects) and opened full-screen
    /// by <see cref="ProjectViewer"/>. Slides: cover → gallery → exploded view.
    /// </summary>
    [CreateAssetMenu(menuName = "Portfolio/Project", fileName = "Project")]
    public class ProjectData : ScriptableObject
    {
        [Header("Card")]
        public string title;
        [Tooltip("One sentence: what it is and for whom.")]
        public string tagline;
        public string year;
        [Tooltip("e.g. Consumer product, Furniture, Lighting, Medical")]
        public string category;
        [Tooltip("The hero shot: the finished product, clean background. Also the card thumbnail.")]
        public Sprite cover;
        public Color accent = new(0.42f, 0.62f, 0.95f);

        [Header("Images (2-5)")]
        [Tooltip("Story order: context/use, sketches, models/prototypes, details, final.")]
        public List<ProjectImage> gallery = new();
        public Sprite explodedView;
        public string explodedCaption;

        [Header("Story")]
        [TextArea(3, 10), Tooltip("The problem / brief / insight.")]
        public string challenge;
        [TextArea(3, 10), Tooltip("What you designed and why it solves it.")]
        public string solution;
        [TextArea(3, 10), Tooltip("Research, iterations, testing. Optional.")]
        public string process;

        [Header("Details")]
        public string role;
        public string duration;
        [Tooltip("Client, course, competition or personal project.")]
        public string context;
        [Tooltip("e.g. SolidWorks, KeyShot, Fusion 360, Bambu printers, CNC")]
        public string tools;
        public List<ProjectFact> facts = new();
        public List<StationLink> links = new();
    }
}
