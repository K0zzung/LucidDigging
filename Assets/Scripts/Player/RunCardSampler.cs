using System;
using System.Collections.Generic;

public static class RunCardSampler
{
    public static bool CanDrawThree(IReadOnlyList<RunCardData> pool,
        RunCardRaritySettings settings, out string error) {
        return TryGetEligible(pool, settings, out _, out error);
    }

    public static bool TryDrawThree(IReadOnlyList<RunCardData> pool,
        RunCardRaritySettings settings, out RunCardData[] result, out string error,
        System.Random random = null) {
        result = null;
        if (!TryGetEligible(pool, settings, out List<RunCardData> remaining, out error))
            return false;

        var chosen = new RunCardData[3];
        for (int slot = 0; slot < chosen.Length; slot++) {
            var rarities = new List<RunCardRarity>();
            double totalWeight = 0;
            foreach (RunCardData card in remaining) {
                if (rarities.Contains(card.Rarity)) continue;
                rarities.Add(card.Rarity);
                totalWeight += settings.GetEntry(card.Rarity).weight;
            }

            double roll = (random != null ? random.NextDouble() : UnityEngine.Random.value) * totalWeight;
            RunCardRarity rarity = rarities[rarities.Count - 1];
            foreach (RunCardRarity candidate in rarities) {
                double weight = settings.GetEntry(candidate).weight;
                if (roll < weight) { rarity = candidate; break; }
                roll -= weight;
            }

            List<RunCardData> inRarity = remaining.FindAll(card => card.Rarity == rarity);
            int index = random != null ? random.Next(inRarity.Count) : UnityEngine.Random.Range(0, inRarity.Count);
            RunCardData selected = inRarity[index];
            chosen[slot] = selected;
            remaining.RemoveAll(card => card.BaseCardId == selected.BaseCardId);
        }
        result = chosen;
        return true;
    }

    private static bool TryGetEligible(IReadOnlyList<RunCardData> pool,
        RunCardRaritySettings settings, out List<RunCardData> eligible, out string error) {
        eligible = new List<RunCardData>();
        error = null;
        if (settings == null) { error = "Rarity settings are missing."; return false; }
        if (!settings.TryValidate(out error)) return false;
        var ids = new HashSet<string>();
        var families = new HashSet<string>();
        int missingReferences = 0;
        int invalidData = 0;
        int excludedRarities = 0;
        if (pool != null) {
            foreach (RunCardData card in pool) {
                if (card == null) { missingReferences++; continue; }
                if (!card.HasValidEffect || string.IsNullOrWhiteSpace(card.Id) ||
                    string.IsNullOrWhiteSpace(card.BaseCardId)) { invalidData++; continue; }
                if (!settings.IsEligible(card.Rarity)) { excludedRarities++; continue; }
                if (settings.GetFrame(card.Rarity) == null) {
                    error = $"Missing frame for eligible rarity {card.Rarity}.";
                    return false;
                }
                if (!ids.Add(card.Id)) {
                    // Repeated references do not change probabilities; ambiguous IDs are rejected.
                    if (eligible.Contains(card)) continue;
                    error = $"Duplicate card ID: {card.Id}.";
                    return false;
                }
                eligible.Add(card);
                families.Add(card.BaseCardId);
            }
        }
        if (families.Count < 3) {
            error = "At least three eligible baseCardId families are required. Rewards were not consumed. " +
                $"Pool={pool?.Count ?? 0}, eligible={eligible.Count}, families={families.Count}, " +
                $"missingRefs={missingReferences}, invalidData={invalidData}, disabledOrZeroWeight={excludedRarities}.";
            return false;
        }
        return true;
    }
}
