using DG.Tweening;
using UnityEngine;

public class MainMenuUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject readyUI;
    [SerializeField] private GameObject playButtonRoot;

    [SerializeField] private GameObject gameHUD;

    [Header("Controllers")]
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private GameCameraController cameraController;

    [Header("Camera")]
    [SerializeField] private float cameraBlendTime = 0.6f;

    [SerializeField] private HUDController hudController;

    private void Start() {
        menuRoot.SetActive(true);

        readyUI.SetActive(true);
        playButtonRoot.SetActive(false);

        if (gameHUD != null)
            gameHUD.SetActive(false);

        GameFlowController.Instance.SetState(
            GameState.Ready);
    }

    public void OnReadyPressed() {
        // 타이틀 + READY 제거
        readyUI.SetActive(false);

        // Inventory 내려오기
        inventoryUI.Open();

        // PLAY 등장
        playButtonRoot.SetActive(true);

        GameFlowController.Instance.SetState(
            GameState.Inventory);
    }

    public void OnPlayPressed() {
        // 중복 입력 방지
        playButtonRoot.SetActive(false);

        // Inventory부터 접는다.
        inventoryUI.Close(() => {
            StartGameplayTransition();
        });
    }
    public void ReturnToMenu() {
        menuRoot.SetActive(true);

        readyUI.SetActive(false);
        playButtonRoot.SetActive(true);

        if (gameHUD != null) {
            gameHUD.SetActive(false);
        }

        GameFlowController.Instance.SetState(
            GameState.Inventory);
    }
    private void StartGameplayTransition() {
        // Overlay로 화면을 덮고 있던 메뉴 제거
        menuRoot.SetActive(false);

        // Gameplay Cinemachine Camera로 전환
        cameraController.SwitchToGameplay();

        // HUD 표시
        if (gameHUD != null) {
            gameHUD.SetActive(true);
        }

        if (hudController != null) {
            hudController.ShowGameplayHUD();
        }

        // 카메라 이동이 어느 정도 끝난 뒤 조작 가능
        DOVirtual.DelayedCall(
            cameraBlendTime,
            () => {
                GameFlowController.Instance.SetState(
                    GameState.Playing);
            });
    }
}