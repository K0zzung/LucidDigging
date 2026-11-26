using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugSaveResetter : MonoBehaviour
{
    // Setting
    [Header("Debug")]
    [Tooltip("Play 중 이 키를 누르면 모든 저장 데이터를 초기화합니다.")]
    [SerializeField] private KeyCode resetKey = KeyCode.F8;

    // Unity

    private void Update() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (Input.GetKeyDown(
            resetKey)) {
            ResetAllSaveData();
        }

#endif
    }


    // Reset

    [ContextMenu("DEBUG / Reset All Save Data")]
    public void ResetAllSaveData() {
        Debug.LogWarning("[DEBUG] 모든 저장 데이터를 초기화합니다.");
        // 모든 PlayerPrefs 삭제

        PlayerPrefs.DeleteAll();

        PlayerPrefs.Save();


        Debug.LogWarning("[DEBUG] PlayerPrefs 삭제 완료.");

        // 현재 Scene Reload

        Scene currentScene = SceneManager.GetActiveScene();


        SceneManager.LoadScene(currentScene.buildIndex);
    }
}