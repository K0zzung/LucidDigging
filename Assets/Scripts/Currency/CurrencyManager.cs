using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance {
        get;
        private set;
    }


    // =========================================================
    // Permanent Currency
    // =========================================================

    public int DashCurrency {
        get;
        private set;
    }

    public int ShockwaveCurrency {
        get;
        private set;
    }

    public int ShieldCurrency {
        get;
        private set;
    }


    // =========================================================
    // Run Currency
    // =========================================================

    public int RunDashCurrency {
        get;
        private set;
    }

    public int RunShockwaveCurrency {
        get;
        private set;
    }

    public int RunShieldCurrency {
        get;
        private set;
    }


    public event Action OnCurrencyChanged;


    // 같은 Run을 두 번 정산하는 것 방지
    private bool runCurrencySettled;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake() {
        Instance =
            this;


        Load();


        ResetRunCurrency();
    }


    // =========================================================
    // Add
    // =========================================================

    public void AddCurrency(
        CurrencyType type,
        int amount) {
        if (amount <= 0)
            return;


        // =============================
        // 이번 Run 획득량에만 추가
        // =============================

        switch (type) {
            case CurrencyType.Dash:

                RunDashCurrency +=
                    amount;

                break;


            case CurrencyType.Shockwave:

                RunShockwaveCurrency +=
                    amount;

                break;


            case CurrencyType.Shield:

                RunShieldCurrency +=
                    amount;

                break;
        }


        OnCurrencyChanged?.Invoke();
    }


    // =========================================================
    // Run Settlement
    // =========================================================

    public void SettleRunCurrency() {
        // 같은 Run 중복 정산 방지
        if (runCurrencySettled)
            return;


        runCurrencySettled =
            true;


        // =============================
        // Run → Permanent
        // =============================

        DashCurrency +=
            RunDashCurrency;


        ShockwaveCurrency +=
            RunShockwaveCurrency;


        ShieldCurrency +=
            RunShieldCurrency;


        // 영구 저장
        Save();


        Debug.Log(
            "[Currency] Run Settled / " +
            $"Dash +{RunDashCurrency} / " +
            $"Shockwave +{RunShockwaveCurrency} / " +
            $"Shield +{RunShieldCurrency}");


        OnCurrencyChanged?.Invoke();
    }


    // =========================================================
    // Spend
    // =========================================================

    public bool SpendCurrency(
        CurrencyType type,
        int amount) {
        if (amount <= 0)
            return false;


        int current =
            GetCurrency(
                type);


        if (current < amount)
            return false;


        switch (type) {
            case CurrencyType.Dash:

                DashCurrency -=
                    amount;

                break;


            case CurrencyType.Shockwave:

                ShockwaveCurrency -=
                    amount;

                break;


            case CurrencyType.Shield:

                ShieldCurrency -=
                    amount;

                break;
        }


        Save();


        OnCurrencyChanged?.Invoke();


        return true;
    }


    // =========================================================
    // Get Permanent
    // =========================================================

    public int GetCurrency(
        CurrencyType type) {
        switch (type) {
            case CurrencyType.Dash:

                return DashCurrency;


            case CurrencyType.Shockwave:

                return ShockwaveCurrency;


            case CurrencyType.Shield:

                return ShieldCurrency;
        }


        return 0;
    }


    // =========================================================
    // Get Run
    // =========================================================

    public int GetRunCurrency(
        CurrencyType type) {
        switch (type) {
            case CurrencyType.Dash:

                return RunDashCurrency;


            case CurrencyType.Shockwave:

                return RunShockwaveCurrency;


            case CurrencyType.Shield:

                return RunShieldCurrency;
        }


        return 0;
    }


    // =========================================================
    // Reset Run
    // =========================================================

    public void ResetRunCurrency() {
        RunDashCurrency =
            0;


        RunShockwaveCurrency =
            0;


        RunShieldCurrency =
            0;


        runCurrencySettled =
            false;


        OnCurrencyChanged?.Invoke();
    }


    // =========================================================
    // Save / Load
    // =========================================================

    private void Load() {
        DashCurrency =
            PlayerPrefs.GetInt(
                "Currency_Dash",
                0);


        ShockwaveCurrency =
            PlayerPrefs.GetInt(
                "Currency_Shockwave",
                0);


        ShieldCurrency =
            PlayerPrefs.GetInt(
                "Currency_Shield",
                0);
    }


    private void Save() {
        PlayerPrefs.SetInt(
            "Currency_Dash",
            DashCurrency);


        PlayerPrefs.SetInt(
            "Currency_Shockwave",
            ShockwaveCurrency);


        PlayerPrefs.SetInt(
            "Currency_Shield",
            ShieldCurrency);


        PlayerPrefs.Save();
    }
}