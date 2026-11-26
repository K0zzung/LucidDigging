using TMPro;
using UnityEngine;

public class CurrencyBarUI : MonoBehaviour
{
    [Header("Currency Text")]

    [SerializeField]
    private TMP_Text dashCurrencyText;

    [SerializeField]
    private TMP_Text shockwaveCurrencyText;

    [SerializeField]
    private TMP_Text shieldCurrencyText;


    private bool isBound;


    // =========================================================
    // Unity
    // =========================================================

    private void OnEnable() {
        BindCurrencyManager();

        Refresh();
    }


    private void Start() {
        // OnEnable 시점에 CurrencyManager.Awake가
        // 아직 실행되지 않은 경우를 대비
        BindCurrencyManager();

        Refresh();
    }


    private void OnDisable() {
        UnbindCurrencyManager();
    }


    // =========================================================
    // Bind
    // =========================================================

    private void BindCurrencyManager() {
        if (isBound)
            return;


        if (CurrencyManager.Instance == null)
            return;


        CurrencyManager.Instance.OnCurrencyChanged +=
            Refresh;


        isBound =
            true;
    }


    private void UnbindCurrencyManager() {
        if (!isBound)
            return;


        if (CurrencyManager.Instance != null) {
            CurrencyManager.Instance.OnCurrencyChanged -=
                Refresh;
        }


        isBound =
            false;
    }


    // =========================================================
    // Refresh
    // =========================================================

    private void Refresh() {
        if (CurrencyManager.Instance == null)
            return;


        if (dashCurrencyText != null) {
            dashCurrencyText.text =
                CurrencyManager.Instance
                    .DashCurrency
                    .ToString();
        }


        if (shockwaveCurrencyText != null) {
            shockwaveCurrencyText.text =
                CurrencyManager.Instance
                    .ShockwaveCurrency
                    .ToString();
        }


        if (shieldCurrencyText != null) {
            shieldCurrencyText.text =
                CurrencyManager.Instance
                    .ShieldCurrency
                    .ToString();
        }
    }
}