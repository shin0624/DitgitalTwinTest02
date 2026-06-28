using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지도 위 선택 위치 마커의 파동(pulse) 링 애니메이션.
/// ring1, ring2 는 각각 Image 컴포넌트를 가진 자식 오브젝트.
/// ring2Delay 만큼 위상 차이를 주어 이중 파동 효과를 만든다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PulseMarkerUI : MonoBehaviour
{
    [Header("링 Image 오브젝트")]
    [SerializeField] private Image ring1;
    [SerializeField] private Image ring2;

    [Header("파라미터")]
    [SerializeField] private float duration    = 2.5f;  // 1사이클 (초)
    [SerializeField] private float ring2Delay  = 0.9f;  // ring2 딜레이 (초)
    [SerializeField] private float maxScale    = 3.0f;  // 최대 스케일 배율
    [SerializeField] private Color ringColor   = new Color(0f, 0.831f, 1f, 0.55f); // #00d4ffcc

    void OnEnable()
    {
        if (ring1) StartCoroutine(PulseLoop(ring1, 0f));
        if (ring2) StartCoroutine(PulseLoop(ring2, ring2Delay));
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ResetRing(ring1);
        ResetRing(ring2);
    }

    private IEnumerator PulseLoop(Image ring, float initialDelay)
    {
        ring.gameObject.SetActive(false);
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);
        ring.gameObject.SetActive(true);

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // scale: 1 → maxScale
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, maxScale, t);
                // alpha: ringColor.a → 0
                Color c = ringColor;
                c.a = Mathf.Lerp(ringColor.a, 0f, t);
                ring.color = c;

                elapsed += Time.deltaTime;
                yield return null;
            }
            ResetRing(ring);
        }
    }

    private void ResetRing(Image ring)
    {
        if (!ring) return;
        ring.rectTransform.localScale = Vector3.one;
        ring.color = ringColor;
    }
}
