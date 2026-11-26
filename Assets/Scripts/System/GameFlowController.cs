using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance {
        get;
        private set;
    }


    public GameState CurrentState {
        get;
        private set;
    }


    [Header("Player")]

    [SerializeField]
    private PlayerGridController playerController;

    [SerializeField]
    private PlayerStatus playerStatus;


    [Header("Camera")]

    [SerializeField]
    private GameCameraController cameraController;


    [Header("Game Over")]

    //private float deathWaitDuration = 1f;

    [Tooltip("카메라 전환 후 결과 UI를 띄우기까지 기다리는 시간")]
    [SerializeField]
    private float cameraReturnDuration = 1.5f;


    //private bool gameOverStarted;
    
    [Header("Player Death")]

    [SerializeField]
    private Animator playerAnimator;

    [SerializeField]
    private string deathTriggerName = "Die";

    [SerializeField]
    private float deathAnimationDuration = 1.2f;


    [Header("Background")]

    [SerializeField]
    private DepthBackgroundController depthBackgroundController;
    [SerializeField] private HUDController hudController;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private DepthManager depthManager;

    [SerializeField]
    private DepthWarningUI depthWarningUI;

    private bool isRetrying;
    private static readonly KeyCode[] keyboardKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private void Awake() {
        Instance =
            this;
    }


    private void Start() {
        if (playerStatus != null) {
            playerStatus.OnPlayerDied +=
                HandlePlayerDied;
        }


        SetState(
            GameState.Ready);
    }

    private void Update() {
        if (isRetrying)
            return;


        if (gameOverUI == null)
            return;


        // GameOver 모든 연출
        // ↓
        // 1.5초 대기
        // ↓
        // Press any key 등장 완료
        // 이 모든 게 끝나야 true
        if (!gameOverUI.IsReadyForRetry)
            return;


        if (!Input.anyKeyDown)
            return;


        // 마우스 클릭은 Retry에서 제외
        if (IsMouseButtonDown())
            return;


        RetryRun();
    }

    private bool IsMouseButtonDown() {
        for (int i = 0;
             i <= 6;
             i++) {
            if (Input.GetMouseButtonDown(i)) {
                return true;
            }
        }
        return false;
    }
    private bool IsAnyKeyboardKeyDown() {
        foreach (KeyCode key in keyboardKeys) {
            // None 제외
            if (key == KeyCode.None)
                continue;


            // Mouse 및 그 이후 Joystick 계열 제외
            if ((int)key >=(int)KeyCode.Mouse0) {
                continue;
            }


            if (Input.GetKeyDown(
                key)) {
                return true;
            }
        }


        return false;
    }
    private void RetryRun() {
        if (isRetrying)
            return;


        isRetrying =
            true;


        Debug.Log(
            "[GameFlow] Retry");


        Time.timeScale =
            1f;


        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex);
    }
    private void OnDestroy() {
        if (playerStatus != null) {
            playerStatus.OnPlayerDied -=
                HandlePlayerDied;
        }


        if (Instance == this) {
            Instance =
                null;
        }
    }


    // =========================================================
    // State
    // =========================================================

    public void SetState(
        GameState state) {
        CurrentState =
            state;


        switch (state) {
            case GameState.Ready:

                playerController.SetInputEnabled(
                    false);

                break;


            case GameState.Inventory:

                playerController.SetInputEnabled(
                    false);

                break;


            case GameState.Playing:

                playerController.SetInputEnabled(
                    true);

                break;


            case GameState.GameOver:

                playerController.SetInputEnabled(
                    false);

                break;
        }


        Debug.Log(
            $"[GameFlow] State -> {state}");
    }


    // =========================================================
    // Player Death
    // =========================================================

    private void HandlePlayerDied() {
        // 이미 GameOver 처리 중이면 중복 실행 방지
        if (CurrentState == GameState.GameOver)
            return;


        // =============================
        // GameOver 상태
        // =============================

        SetState(GameState.GameOver);


        // =============================
        // HUD 사망 연출
        // =============================

        if (hudController != null) {
            hudController.HideGameplayHUD();
        }

        if (depthWarningUI != null) {
            depthWarningUI.HideImmediately();
        }

        // =============================
        // GameOver Audio
        // BGM 제외 SFX / UI Mute
        // =============================

        if (AudioManager.Instance != null) {
            AudioManager.Instance
                .EnterGameOverAudio();
        }

        // =============================
        // 이번 Run Currency 영구 정산
        // =============================

        if (CurrencyManager.Instance != null) {
            CurrencyManager.Instance
                .SettleRunCurrency();
        }


        // =============================
        // 기존 GameOver 연출
        // =============================

        StartCoroutine(
            GameOverRoutine());
    }


    // =========================================================
    // Game Over
    // =========================================================

    private IEnumerator GameOverRoutine() {
        Debug.Log(
            "[GameFlow] Player Death");


        // =====================================================
        // 1. 캐릭터 사망 애니메이션
        // =====================================================

        if (playerAnimator != null) {
            playerAnimator.SetTrigger(
                deathTriggerName);
        }


        // =====================================================
        // 2. 동시에 Background Death Flash
        //
        // RED → WHITE → RED → WHITE
        // =====================================================

        if (depthBackgroundController != null) {
            depthBackgroundController.PlayDeathFlash();
        }


        // =====================================================
        // 3. 캐릭터가 쓰러지는 동안 기다림
        // =====================================================

        yield return new WaitForSeconds(
            deathAnimationDuration);


        // =====================================================
        // 4. 카메라를 게임 시작 지점으로 복귀
        // =====================================================

        if (cameraController != null) {
            cameraController.SwitchToMenu();
        }


        // =====================================================
        // 5. 카메라가 올라가는 동안
        // Background도 Depth 0 색으로 변화
        // =====================================================

        if (depthBackgroundController != null) {
            depthBackgroundController.ReturnToStartColor(
                cameraReturnDuration);
        }


        Debug.Log(
            "[GameFlow] Camera Returning");


        // Cinemachine Blend 기다림
        yield return new WaitForSeconds(
            cameraReturnDuration);


        // =====================================================
        // 6. Game Over UI
        // =====================================================

        Debug.Log("[GameFlow] Show Game Over UI");

        if (gameOverUI != null) {
            gameOverUI.Show(depthManager.CurrentDepth);
        }
    }


    // =========================================================
    // Play
    // =========================================================

    public void StartGame() {
        cameraController.SwitchToGameplay();

        SetState(
            GameState.Playing);
    }
}