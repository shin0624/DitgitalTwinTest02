using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Michsky.UI.Heat;

/// <summary>
/// 지도 클릭 시 "패널 설치 확인" 대화상자를 표시하고,
/// 역지오코딩으로 한국 도로명주소를 조회하는 컴포넌트.
/// </summary>
public class LocationConfirmPanel : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private ModalWindowManager modalWindow;
    [SerializeField] private TMP_Text questionText;  // 본문 텍스트

    [Header("역지오코딩")]
    [Tooltip("Node.js 프록시의 역지오코딩 엔드포인트.\n예) http://localhost:3000/reverse-geocode")]
    [SerializeField] private string reverseGeocodeUrl = "http://localhost:3000/reverse-geocode";

    public bool IsVisible => modalWindow != null && modalWindow.isOn;

    private Action _onYes;
    private Action _onNo;

    void Awake()
    {
        if (modalWindow != null)
        {
            modalWindow.onConfirm.AddListener(OnYes);
            modalWindow.onCancel.AddListener(OnNo);
        }
    }

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>패널을 표시하고 주소를 비동기 조회합니다.</summary>
    public void Show(double lat, double lon, Action onYes, Action onNo)
    {
        _onYes = onYes;
        _onNo  = onNo;

        if (modalWindow == null)
        {
            Debug.LogError("[LocationConfirmPanel] modalWindow가 null입니다. Inspector에서 ModalWindowManager를 연결하세요.");
            onYes?.Invoke();
            return;
        }

        SetQuestion($"{lat:F4}°N,  {lon:F4}°E", isLoading: true);
        modalWindow.OpenWindow();

        StopAllCoroutines();
        StartCoroutine(FetchAndShow(lat, lon));
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (modalWindow != null) modalWindow.CloseWindow();
    }

    // ── 버튼 콜백 ─────────────────────────────────────────────
    // ModalWindowManager의 closeOnConfirm/closeOnCancel이 창 닫기를 처리함

    private void OnYes() => _onYes?.Invoke();
    private void OnNo()  => _onNo?.Invoke();

    // ── 역지오코딩 ────────────────────────────────────────────

    private IEnumerator FetchAndShow(double lat, double lon)
    {
        if (string.IsNullOrWhiteSpace(reverseGeocodeUrl))
        {
            Debug.LogWarning("[LocationConfirmPanel] reverseGeocodeUrl이 비어 있습니다. Inspector에서 프록시 URL을 설정하세요.");
            yield break;
        }

        string url = $"{reverseGeocodeUrl.TrimEnd('/')}?lat={lat:F6}&lon={lon:F6}";
        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = 5;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string address = ParseAddress(req.downloadHandler.text);
            if (!string.IsNullOrEmpty(address))
            {
                SetQuestion(address, isLoading: false);
                yield break;
            }
        }

        // 조회 실패 시 좌표 그대로 유지
        SetQuestion($"{lat:F4}°N,  {lon:F4}°E", isLoading: false);
    }

    // ── JSON 파싱 ─────────────────────────────────────────────

    private string ParseAddress(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            // VWORLD 프록시 응답: { "address": "서울특별시 강남구 테헤란로 212" }
            return GetJsonString(json, "address");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LocationConfirmPanel] JSON 파싱 오류: {e.Message}");
            return null;
        }
    }

    private static string GetJsonString(string json, string key)
    {
        string searchKey = $"\"{key}\"";
        int keyIdx = json.IndexOf(searchKey, StringComparison.Ordinal);
        if (keyIdx < 0) return null;

        int colon = json.IndexOf(':', keyIdx + searchKey.Length);
        if (colon < 0) return null;

        int i = colon + 1;
        while (i < json.Length && json[i] == ' ') i++;
        if (i >= json.Length || json[i] != '"') return null;

        int start = i + 1;
        int end   = start;
        while (end < json.Length)
        {
            if (json[end] == '"' && json[end - 1] != '\\') break;
            end++;
        }
        return end >= json.Length ? null : json.Substring(start, end - start);
    }

    // ── UI 헬퍼 ───────────────────────────────────────────────

    private void SetQuestion(string locationStr, bool isLoading)
    {
        if (!questionText) return;
        string loadingMark = isLoading ? " (조회 중...)" : "";
        questionText.text =
            $"이 위치에 패널을 설치하시겠습니까?\n" +
            $"현재 위치 : {locationStr}{loadingMark}";
    }
}
