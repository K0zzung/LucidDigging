using Cinemachine;
using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera menuCamera;

    [SerializeField]
    private CinemachineVirtualCamera gameplayCamera;

    public void SwitchToGameplay() {
        menuCamera.Priority = 10;
        gameplayCamera.Priority = 20;

        Debug.Log("Camera -> Gameplay");
    }

    public void SwitchToMenu() {
        menuCamera.Priority = 20;
        gameplayCamera.Priority = 10;
    }
}