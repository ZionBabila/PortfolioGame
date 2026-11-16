using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    /// <summary>One code you hand out, and the CV it unlocks.</summary>
    [Serializable]
    public class CvCode
    {
        [Tooltip("What the visitor types. Not case-sensitive; spaces and dashes are ignored.")]
        public string code;
        [Tooltip("Who you gave it to, as a reminder. It ships inside the build files, so keep it vague (no company names).")]
        public string note;
        [Tooltip("Button text once unlocked. Keep it the same for every code, so no one sees there are two CVs.")]
        public string label = "Download CV";
        [Tooltip("The PDF, relative to the site. Every CV is named ZionBabila_CV.pdf, each in its own neutral folder.")]
        public string url;
    }

    /// <summary>
    /// The CV codes. The About letter shows a code box instead of open download links; each code reveals the CV it
    /// belongs to. Edit Resources/CvAccess in the Inspector: add one code per company if you want to know who's who.
    /// Note: the PDFs are still public files on the site, so this decides what a visitor is offered, it isn't a lock.
    /// </summary>
    [CreateAssetMenu(menuName = "Portfolio/CV Access", fileName = "CvAccess")]
    public class CvAccess : ScriptableObject
    {
        /// <summary>A StationLink with this url becomes the code box.</summary>
        public const string Marker = "#cv-code";

        public List<CvCode> codes = new();

        [TextArea(2, 3)] public string prompt = "Got a code from me? Type it here to download my CV.";
        public string wrongCode = "That code doesn't open anything. Ask me for one.";

        static CvAccess cached;
        public static CvAccess Default
        {
            get
            {
                if (!cached) cached = Resources.Load<CvAccess>("CvAccess");
                return cached;
            }
        }

        public static string Normalize(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace(" ", "").Replace("-", "").Replace("_", "").Trim().ToUpperInvariant();

        public CvCode Find(string typed)
        {
            var key = Normalize(typed);
            if (key.Length == 0) return null;
            foreach (var c in codes)
                if (c != null && Normalize(c.code) == key && !string.IsNullOrEmpty(c.url)) return c;
            return null;
        }
    }
}
