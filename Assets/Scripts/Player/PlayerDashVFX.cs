using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerDashVFX : MonoBehaviour
{
    [Header("Source Renderers")]
    [SerializeField]
    private List<SpriteRenderer> sourceRenderers =
        new List<SpriteRenderer>();


    [Header("After Image")]
    [SerializeField]
    private float spawnInterval = 0.05f;

    [SerializeField]
    private float afterImageLifetime = 0.18f;

    [Range(0f, 1f)]
    [SerializeField]
    private float startAlpha = 0.45f;


    private Coroutine effectCoroutine;

    private bool isPlaying;


    public void Begin() {
        if (isPlaying)
            return;

        isPlaying = true;


        effectCoroutine =
            StartCoroutine(
                AfterImageRoutine());
    }


    public void End() {
        isPlaying = false;

        if (effectCoroutine != null) {
            StopCoroutine(
                effectCoroutine);

            effectCoroutine = null;
        }
    }


    private IEnumerator AfterImageRoutine() {
        while (isPlaying) {
            SpawnAfterImage();

            yield return
                new WaitForSeconds(
                    spawnInterval);
        }
    }


    private void SpawnAfterImage() {
        GameObject root =
            new GameObject(
                "DashAfterImage");


        foreach (SpriteRenderer source
                 in sourceRenderers) {
            if (source == null)
                continue;

            if (source.sprite == null)
                continue;


            GameObject piece =
                new GameObject(
                    source.name + "_AfterImage");


            piece.transform.SetParent(
                root.transform);


            // ==========================
            // 현재 Sprite의 World 상태 복사
            // ==========================

            piece.transform.position =
                source.transform.position;

            piece.transform.rotation =
                source.transform.rotation;

            piece.transform.localScale =
                source.transform.lossyScale;


            SpriteRenderer copy =
                piece.AddComponent<SpriteRenderer>();


            copy.sprite =
                source.sprite;

            copy.flipX =
                source.flipX;

            copy.flipY =
                source.flipY;

            copy.sortingLayerID =
                source.sortingLayerID;

            copy.sortingOrder =
                source.sortingOrder - 1;

            copy.sharedMaterial =
                source.sharedMaterial;


            Color color =
                source.color;

            color.a =
                startAlpha;

            copy.color =
                color;


            // ==========================
            // Fade Out
            // ==========================

            copy.DOFade(
                0f,
                afterImageLifetime);
        }


        Destroy(
            root,
            afterImageLifetime + 0.05f);
    }


    private void OnDisable() {
        End();
    }
}