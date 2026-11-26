using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Transform fill;


    private Vector3 fillFullScale;


    private void Awake() {
        if (fill != null) {
            fillFullScale =
                fill.localScale;
        }
    }


    public void SetRatio(
        float ratio) {
        if (fill == null)
            return;


        ratio =
            Mathf.Clamp01(
                ratio);


        fill.localScale =
            new Vector3(
                fillFullScale.x * ratio,
                fillFullScale.y,
                fillFullScale.z);
    }
}