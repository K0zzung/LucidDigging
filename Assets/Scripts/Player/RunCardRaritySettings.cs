using System;
using UnityEngine;

[CreateAssetMenu(fileName = "RunCardRaritySettings", menuName = "Game/Run Card Rarity Settings")]
public class RunCardRaritySettings : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public RunCardRarity rarity;
        public bool enabled;
        [Min(0)] public int weight;
        public Sprite frame;
    }

    [SerializeField] private Entry[] entries = {
        new Entry { rarity = RunCardRarity.Common, enabled = true, weight = 64 },
        new Entry { rarity = RunCardRarity.Rare, enabled = true, weight = 25 },
        new Entry { rarity = RunCardRarity.Epic, enabled = true, weight = 8 },
        new Entry { rarity = RunCardRarity.Legendary, enabled = true, weight = 2 },
        new Entry { rarity = RunCardRarity.Nightmare, enabled = false, weight = 1 }
    };

    public Entry GetEntry(RunCardRarity rarity) {
        if (entries != null) {
            foreach (Entry entry in entries)
                if (entry != null && entry.rarity == rarity) return entry;
        }
        return null;
    }

    public bool IsEligible(RunCardRarity rarity) {
        Entry entry = GetEntry(rarity);
        return entry != null && entry.enabled && entry.weight > 0;
    }

    public Sprite GetFrame(RunCardRarity rarity) => GetEntry(rarity)?.frame;

    public bool TryValidate(out string error) {
        error = null;
        if (entries == null) { error = "Rarity settings are missing."; return false; }
        var seen = new System.Collections.Generic.HashSet<RunCardRarity>();
        foreach (Entry entry in entries) {
            if (entry == null || !Enum.IsDefined(typeof(RunCardRarity), entry.rarity) ||
                !seen.Add(entry.rarity) || entry.weight < 0) {
                error = "Rarity entries must have unique valid rarities and nonnegative weights.";
                return false;
            }
        }
        return true;
    }

}
