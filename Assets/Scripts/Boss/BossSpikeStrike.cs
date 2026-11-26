using System;
using System.Collections;
using UnityEngine;

public class BossSpikeStrike : MonoBehaviour
{
    [SerializeField]
    private float hitDelay = 0.15f;

    [SerializeField]
    private float lifeTime = 0.65f;


    public void Play(Action onHit) {
        StartCoroutine(
            StrikeRoutine(onHit));
    }


    private IEnumerator StrikeRoutine(
        Action onHit) {
        yield return new WaitForSeconds(
            hitDelay);


        onHit?.Invoke();


        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                lifeTime - hitDelay));


        Destroy(gameObject);
    }
}