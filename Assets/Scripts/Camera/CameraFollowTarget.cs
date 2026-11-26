using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform player;

    private float fixedX;
    private float lowestY;

    private void Start() {
        // 카메라의 X 위치는 시작 위치에서 고정
        fixedX = transform.position.x;

        // 현재 위치를 카메라가 내려간 최저 Y로 설정
        lowestY = transform.position.y;
    }

    private void LateUpdate() {
        // Unity에서는 아래로 내려갈수록 Y 값이 작아진다.
        // 플레이어가 더 아래로 내려갔을 때만 갱신
        if (player.position.y < lowestY) {
            lowestY = player.position.y;
        }

        transform.position = new Vector3(
            fixedX,
            lowestY,
            transform.position.z
        );
    }
}