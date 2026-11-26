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

    [Header("Run Rewards")]
    [SerializeField] private RunCardSelectionUI runCardSelectionUI;
    public event Action<GameState> OnStateChanged;
    private bool ownsLevelUpPause;
    private float timeScaleBeforeLevelUp = 1f;

    public bool CanConfirmRunCard => CurrentState == GameState.LevelUpSelection &&
        playerStatus != null && !playerStatus.IsDead;

    public bool TryBeginLevelUpSelection() {
        if (CurrentState != GameState.Playing || Time.timeScale <= 0f ||
            playerStatus == null || playerStatus.IsDead || playerController == null ||
            playerController.IsActionInProgress || runCardSelectionUI == null ||
            !runCardSelectionUI.IsReady || RunProgression.Instance == null ||
            RunProgression.Instance.PendingLevelUps <= 0)
            return false;

        SetState(GameState.LevelUpSelection);
        return true;
    }

    public void EndLevelUpSelection() {
        if (CurrentState != GameState.LevelUpSelection) return;
        ReleaseLevelUpPause();
        if (playerStatus != null && playerStatus.IsDead) HandlePlayerDied();
        else SetState(GameState.Playing);
    }

    private void ReleaseLevelUpPause() {
        if (!ownsLevelUpPause) return;
        ownsLevelUpPause = false;
        Time.timeScale = timeScaleBeforeLevelUp;
    }

    private bool isRetrying;
    private static readonly KeyCode[] keyboardKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private void Awake() {
        // This scene starts a new run; never inherit a paused scene time scale.
        Time.timeScale = 1f;
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
        ReleaseLevelUpPause();
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
        // A delayed menu callback must not resume a reward selection or game over.
        if (state == GameState.Playing &&
            (CurrentState == GameState.GameOver ||
             (CurrentState == GameState.LevelUpSelection && ownsLevelUpPause &&
              RunProgression.Instance != null && RunProgression.Instance.PendingLevelUps > 0)))
            return;

        if (state != GameState.LevelUpSelection) ReleaseLevelUpPause();
        else if (!ownsLevelUpPause) {
            timeScaleBeforeLevelUp = Time.timeScale;
            ownsLevelUpPause = true;
            Time.timeScale = 0f;
        }

        CurrentState =
            state;


        switch (state) {
            case GameState.Ready:

                playerController.SetInputEnabled(
                    false);

                break;


            case GameState.LevelUpSelection:
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
        OnStateChanged?.Invoke(state);
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