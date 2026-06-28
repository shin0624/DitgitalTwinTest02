using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LocationSetup 씬의 헤더 정보, 효율 칩, 위치 카드를 일괄 관리하는 스크립트.
/// ComparisonEngine / VirtualWeatherController 와 독립적으로 동작.
/// </summary>
public class LocationSetupUIController : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO         siteConfig;
    [SerializeField] private VirtualWeatherDataSO virtualWeather;

    [Header("의존 컴포넌트")]
    [SerializeField] private KoreaMapController   mapController;

    // ── 헤더 우측 ─────────────────────────────────────
    [Header("헤더 - 좌표 칩")]
    [SerializeField] private TMP_Text coordsChipText;     // "37.5172°N · 127.0473°E"

    [Header("헤더 - 위치 상태")]
    [SerializeField] private Image    statusDot;           // 블링킹 원형 점
    [SerializeField] private TMP_Text statusText;          // "위치 선택됨" / "위치 미선택"

    [Header("헤더 - kWp/패널 칩")]
    [SerializeField] private TMP_Text kwpPanelChipText;   // "4.0 kWp · 10패널"

    // ── 효율 칩 (S4 섹션) ─────────────────────────────
    [Header("효율 칩")]
    [SerializeField] private TMP_Text tiltEffText;        // 기울기 효율 %
    [SerializeField] private TMP_Text azEffText;          // 방위각 효율 %
    [SerializeField] private TMP_Text totalEffText;       // 종합 효율 %

    // ── 지도 위 위치 카드 (bottom-left overlay) ────────
    [Header("위치 카드 (지도 오버레이)")]
    [SerializeField] private GameObject locationCard;          // 위치 선택 전에는 비활성
    [SerializeField] private TMP_Text   locationCardCoordsText; // 카드 위경도

    // ── 색상 상수 ──────────────────────────────────────
    private static readonly Color CyanColor   = new Color(0f,    0.831f, 1f,    1f);   // #00d4ff
    private static readonly Color GreenColor  = new Color(0.290f,0.871f, 0.502f,1f);  // #4ade80
    private static readonly Color MutedColor  = new Color(0.533f,0.600f, 0.733f,1f);  // #8899bb

    private float _blinkTimer;

    void Update()
    {
        RefreshHeader();
        RefreshEfficiencyChips();
        AnimateStatusDot();
    }

    // ── 헤더 갱신 ──────────────────────────────────────

    private void RefreshHeader()
    {
        bool hasLoc = mapController != null && mapController.HasLocation;

        if (siteConfig != null)
        {
            if (coordsChipText)
            {
                double lat = siteConfig.latitude, lon = siteConfig.longitude;
                coordsChipText.text =
                    $"{Mathf.Abs((float)lat):F4}°{(lat >= 0 ? "N" : "S")} · " +
                    $"{Mathf.Abs((float)lon):F4}°{(lon >= 0 ? "E" : "W")}";
            }

            if (kwpPanelChipText)
            {
                float kwp = siteConfig.panelCount * 0.4f;
                kwpPanelChipText.text = $"{kwp:F1} kWp · {siteConfig.panelCount}패널";
            }

            if (locationCardCoordsText)
                locationCardCoordsText.text =
                    $"{siteConfig.latitude:F4}°N,  {siteConfig.longitude:F4}°E";
        }

        if (statusText)
        {
            statusText.text  = hasLoc ? "위치 선택됨" : "위치 미선택";
            statusText.color = hasLoc ? GreenColor : MutedColor;
        }

        if (locationCard)
            locationCard.SetActive(hasLoc);
    }

    // ── 효율 칩 갱신 ───────────────────────────────────

    private void RefreshEfficiencyChips()
    {
        if (siteConfig == null) return;

        float tiltEff  = Mathf.Max(0.52f, 1f - Mathf.Abs(siteConfig.tiltAngleDeg - 35f) * 0.007f);
        float azEff    = Mathf.Max(0.62f, Mathf.Cos((siteConfig.azimuthDeg - 180f) * Mathf.Deg2Rad) * 0.18f + 0.82f);
        float cloudM   = virtualWeather != null
                         ? (1f - virtualWeather.cloudCoverPercent / 100f * 0.85f)
                         : 1f;
        float totalEff = tiltEff * azEff * cloudM;

        SetText(tiltEffText,  $"{Mathf.RoundToInt(tiltEff  * 100)}%");
        SetText(azEffText,    $"{Mathf.RoundToInt(azEff    * 100)}%");
        SetText(totalEffText, $"{Mathf.RoundToInt(totalEff * 100)}%");
    }

    // ── 상태 점 블링킹 ────────────────────────────────

    private void AnimateStatusDot()
    {
        if (!statusDot) return;

        bool hasLoc = mapController != null && mapController.HasLocation;
        if (!hasLoc)
        {
            statusDot.color = MutedColor;
            return;
        }

        _blinkTimer += Time.deltaTime;
        // 2초 주기: alpha 1.0 ↔ 0.15
        float alpha = Mathf.Lerp(0.15f, 1f, (Mathf.Sin(_blinkTimer * Mathf.PI) + 1f) * 0.5f);
        Color c = GreenColor;
        c.a = alpha;
        statusDot.color = c;
    }

    private void SetText(TMP_Text t, string s) { if (t) t.text = s; }
}
