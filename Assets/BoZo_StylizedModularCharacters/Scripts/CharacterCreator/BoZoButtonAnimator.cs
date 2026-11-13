using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Bozo.ModularCharacters
{


public class BoZoButtonAnimator : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float duration = 0.12f;

        private Vector3 originalScale;
        private Coroutine scaleCoroutine;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartScaleTween(originalScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartScaleTween(originalScale);
        }

        private void StartScaleTween(Vector3 targetScale)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);

            scaleCoroutine = StartCoroutine(ScaleTween(targetScale));
        }

        private IEnumerator ScaleTween(Vector3 targetScale)
        {
            Vector3 startingScale = transform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);

                // Smoothly ease in and out.
                progress = Mathf.SmoothStep(0f, 1f, progress);

                transform.localScale =
                    Vector3.LerpUnclamped(startingScale, targetScale, progress);

                yield return null;
            }

            transform.localScale = targetScale;
            scaleCoroutine = null;
        }

        private void OnDisable()
        {
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
                scaleCoroutine = null;
            }

            transform.localScale = originalScale;
        }
    }
}