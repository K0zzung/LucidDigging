using UnityEngine;

public class CurrencyPickup : GridPickup
{
    [SerializeField]
    private CurrencyType currencyType;

    [SerializeField]
    private int amount = 1;


    protected override bool TryApplyPickupEffect() {
        if (CurrencyManager.Instance == null) {
            Debug.LogError(
                "[CurrencyPickup] CurrencyManager가 없습니다.");

            return false;
        }


        CurrencyManager.Instance.AddCurrency(currencyType,amount);


        // =============================
        // Pickup Sound
        // =============================

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlayItemPickup();
        }
        return true;
    }
}