using System;
using UnityEngine;

namespace Portfolio.Slot
{
    /// <summary>Who the visitor plays when they walk into the studio. Picked on the welcome screen.</summary>
    public enum VisitorRole { Neutral, HR, TeamLead, HeadOfDepartment }

    /// <summary>The visitor's chosen role, kept in PlayerPrefs so a reload remembers it.</summary>
    public static class Visitor
    {
        const string Key = "Portfolio.Visitor.Role";

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Changed = null;

        public static bool Chosen => PlayerPrefs.HasKey(Key);

        public static VisitorRole Current =>
            Enum.TryParse(PlayerPrefs.GetString(Key, nameof(VisitorRole.Neutral)), out VisitorRole r) ? r : VisitorRole.Neutral;

        public static void Choose(VisitorRole role)
        {
            PlayerPrefs.SetString(Key, role.ToString());
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Forget()
        {
            PlayerPrefs.DeleteKey(Key);
            Changed?.Invoke();
        }

        public static string Title(VisitorRole role) => role switch
        {
            VisitorRole.HR => "HR",
            VisitorRole.TeamLead => "Team Lead",
            VisitorRole.HeadOfDepartment => "Head of Department",
            _ => "Just visiting",
        };

        public static string Blurb(VisitorRole role) => role switch
        {
            VisitorRole.HR => "You care about people, culture and fit.",
            VisitorRole.TeamLead => "You want to see how I build things.",
            VisitorRole.HeadOfDepartment => "You're looking for impact and results.",
            _ => "Curious. No agenda.",
        };
    }
}
