using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Portfolio.Slot
{
    /// <summary>Spin energy and the facts already revealed, kept in PlayerPrefs so a reload continues.</summary>
    public static class Energy
    {
        const string EnergyKey = "Portfolio.Slot.Energy";
        const string RevealedKey = "Portfolio.Slot.Revealed";

        static int amount = -1;
        static HashSet<string> revealed;

        public static event Action Changed;
        /// <summary>Energy just gained, for the "+2 energy" note.</summary>
        public static event Action<int> Gained;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { amount = -1; revealed = null; Changed = null; Gained = null; }

        public static int Amount
        {
            get
            {
                if (amount < 0)
                {
                    var data = SlotMachineData.Default;
                    amount = PlayerPrefs.GetInt(EnergyKey, data ? data.startEnergy : 3);
                }
                return amount;
            }
        }

        public static void Add(int value)
        {
            amount = Mathf.Max(0, Amount + value);
            PlayerPrefs.SetInt(EnergyKey, amount);
            PlayerPrefs.Save();
            if (value > 0) Gained?.Invoke(value);
            Changed?.Invoke();
        }

        public static bool TrySpend(int value)
        {
            if (Amount < value) return false;
            Add(-value);
            return true;
        }

        static HashSet<string> Revealed => revealed ??= new HashSet<string>(PlayerPrefs.GetString(RevealedKey, "").Split('|').Where(s => s.Length > 0));

        public static bool IsRevealed(string id) => Revealed.Contains(id);
        public static int RevealedCount => Revealed.Count;

        public static void Reveal(string id)
        {
            if (!Revealed.Add(id)) return;
            PlayerPrefs.SetString(RevealedKey, string.Join("|", Revealed));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(EnergyKey);
            PlayerPrefs.DeleteKey(RevealedKey);
            amount = -1; revealed = null;
            Changed?.Invoke();
        }
    }
}
