using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField]
    private PlayerStatus playerStatus;

    [SerializeField]
    private Image hpFill;


    private void OnEnable() {
        if (playerStatus == null)
            return;


        playerStatus.OnHPChanged +=
            HandleHPChanged;


        Refresh();
    }


    private void OnDisable() {
        if (playerStatus != null) {
            playerStatus.OnHPChanged -=
                HandleHPChanged;
        }
    }


    private void Refresh() {
        if (playerStatus == null)
            return;


        HandleHPChanged(
            playerStatus.CurrentHP,
            playerStatus.MaxHP);
    }


    private void HandleHPChanged(
        int currentHP,
        int maxHP) {
        if (hpFill == null)
            return;


        if (maxHP <= 0) {
            hpFill.fillAmount = 0f;
            return;
        }


        hpFill.fillAmount =
            Mathf.Clamp01(
                currentHP
                / (float)maxHP);
    }
}