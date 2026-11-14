using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Slot
{
    /// <summary>One reel icon and the sentence a triple of it reveals.</summary>
    [Serializable]
    public class SlotSymbol
    {
        public string id;
        public Sprite icon;
        [TextArea(2, 4), Tooltip("One short sentence about Zion, revealed when three of this icon line up (\"Just visiting\").")]
        public string fact;
        [Header("Same icon, told to each visitor (empty = use the line above)")]
        [TextArea(2, 4)] public string factHR;
        [TextArea(2, 4)] public string factTeamLead;
        [TextArea(2, 4)] public string factHead;
        [Header("Tech-art link (?for=techart): Tech Art Team Lead / Head of Tech Art")]
        [TextArea(2, 4)] public string factTechLead;
        [TextArea(2, 4)] public string factTechHead;

        public string FactFor(VisitorRole role)
        {
            bool techArt = Profile.Current == Profile.TechArt;
            var s = role switch
            {
                VisitorRole.HR => factHR,
                VisitorRole.TeamLead => techArt && !string.IsNullOrWhiteSpace(factTechLead) ? factTechLead : factTeamLead,
                VisitorRole.HeadOfDepartment => techArt && !string.IsNullOrWhiteSpace(factTechHead) ? factTechHead : factHead,
                _ => fact,
            };
            return string.IsNullOrWhiteSpace(s) ? fact : s;
        }
    }

    /// <summary>
    /// The fortune machine: its art, icons and facts, and the energy economy. Lives in Resources so the game
    /// finds it without scene changes. Built by Portfolio → Slot → Build Slot Machine Content.
    /// </summary>
    [CreateAssetMenu(menuName = "Portfolio/Slot Machine", fileName = "SlotMachine")]
    public class SlotMachineData : ScriptableObject
    {
        public List<SlotSymbol> symbols = new();

        [Header("Art")]
        public Sprite body;
        public Sprite window;
        public Sprite lever;
        public Sprite energyIcon;

        [Header("Sound (synthesized by Art/Audio/tools/slot_sfx.py)")]
        public AudioClip pullSound;
        public AudioClip tickSound;
        public AudioClip stopSound;
        public AudioClip winSound;
        public AudioClip emptySound;
        public AudioClip catchSound;

        [Header("Energy")]
        public int startEnergy = 3;
        public int spinCost = 1;
        [Tooltip("Energy for catching Zion in the studio.")] public int energyPerCatch = 2;

        [Header("Odds")]
        [Range(0f, 1f), Tooltip("Chance of three of a kind on a spin.")] public float tripleChance = 0.22f;
        [Range(0f, 0.5f), Tooltip("Added to the chance for every spin without a triple.")] public float pityPerMiss = 0.12f;
        [Range(0f, 1f), Tooltip("How often a triple picks a fact not seen yet.")] public float preferNew = 0.85f;

        [TextArea(2, 4)] public string allRevealed = "That's all six. Now you know me. Let's talk.";
        [TextArea(2, 4)] public string allRevealedHR = "That's all six. Shall we schedule the interview?";
        [TextArea(2, 4)] public string allRevealedTeamLead = "That's all six. Want to see the code?";
        [TextArea(2, 4)] public string allRevealedHead = "That's all six. Let's talk about what I can build for you.";

        public string AllRevealedFor(VisitorRole role) => role switch
        {
            VisitorRole.HR => allRevealedHR,
            VisitorRole.TeamLead => allRevealedTeamLead,
            VisitorRole.HeadOfDepartment => allRevealedHead,
            _ => allRevealed,
        };

        static SlotMachineData cached;
        public static SlotMachineData Default
        {
            get
            {
                if (!cached) cached = Resources.Load<SlotMachineData>("SlotMachine");
                return cached;
            }
        }
    }
}
