using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Michsky.UI.Heat;

public class ComparisonEngine : MonoBehaviour
{
    // HTML S3 "시뮬레이션 결과" 섹션과 1:1 대응하는 비교 엔진.
    // "실제 날씨" = 선택 계절의 서울 평균 일사량 기반 기준값.
    // "가상 날씨" = 구름량·온도편차 적용 시나리오.

    [Header("SO 입력")]
    [SerializeField] private RealWeatherDataSO  realWeather;
    [SerializeField] private VirtualWeatherDataSO virtualWeather;
    [SerializeField] private SiteConfigSO       siteConfig;
    [SerializeField] private PanelConfig        panelConfig;

    [Header("S3 테이블 - 일사량 행 (kWh/m²/d)")]
    [SerializeField] private TMP_Text realIrradianceText;
    [SerializeField] private TMP_Text virtIrradianceText;
    [SerializeField] private TMP_Text dIrrText;

    [Header("S3 테이블 - 일일 발전량 행 (kWh/day)")]
    [SerializeField] private TMP_Text realDailyKwhText;
    [SerializeField] private TMP_Text virtDailyKwhText;
    [SerializeField] private TMP_Text dDailyText;

    [Header("S3 테이블 - 연간 발전량 행 (MWh/year)")]
    [SerializeField] private TMP_Text realAnnualMwhText;
    [SerializeField] private TMP_Text virtAnnualMwhText;
    [SerializeField] private TMP_Text dAnnualText;

    [Header("S3 테이블 - 연간 수익 행 (KRW/year)")]
    [SerializeField] private TMP_Text realAnnualRevText;
    [SerializeField] private TMP_Text virtAnnualRevText;
    [SerializeField] private TMP_Text dRevText;

    [Header("발전량 비교 바 (RectTransform anchorMax.x 제어)")]
    [SerializeField] private ProgressBar realBarFill;
    [SerializeField] private ProgressBar virtBarFill;

    [Header("전력 단가")]
    [SerializeField] private float kwhPriceKRW = 130f;

    private static readonly Color GreenColor  = new Color(0.290f, 0.871f, 0.502f, 1f); // #4ade80
    private static readonly Color OrangeColor = new Color(0.984f, 0.573f, 0.235f, 1f); // #fb923c

    void Update()
    {
        CalculateAndDisplay();
    }

    private void CalculateAndDisplay()
    {
        if (virtualWeather == null || siteConfig == null) return;

        float tilt = siteConfig.tiltAngleDeg;
        float az   = siteConfig.azimuthDeg;

        // 효율 계수 (HTML 공식과 동일)
        float tiltEff = Mathf.Max(0.52f, 1f - Mathf.Abs(tilt - 35f) * 0.007f);
        float azEff   = Mathf.Max(0.62f, Mathf.Cos((az - 180f) * Mathf.Deg2Rad) * 0.18f + 0.82f);
        float cloudM  = 1f - virtualWeather.cloudCoverPercent / 100f * 0.85f;
        float tempM   = virtualWeather.tempOffsetC > 0f
                        ? Mathf.Max(0.75f, 1f - virtualWeather.tempOffsetC * 0.005f)
                        : 1f;

        // 용량 및 계절 일사량
        float kwp    = siteConfig.panelCount * 0.4f;
        float seaIrr = virtualWeather.GetSeasonIrradiance(); // kWh/m²/day

        // 실제 날씨 (계절 기준)
        float realIrr    = seaIrr;
        float realDaily  = kwp * seaIrr * tiltEff * azEff;     // kWh/day
        float realAnnual = realDaily * 365f;                    // kWh/year
        float realRev    = realAnnual * kwhPriceKRW;            // KRW/year

        // 가상 날씨
        float virtIrr    = seaIrr * cloudM;
        float virtDaily  = realDaily * cloudM * tempM;
        float virtAnnual = virtDaily * 365f;
        float virtRev    = virtAnnual * kwhPriceKRW;

        // 차이값
        float dIrr   = virtIrr   - realIrr;
        float dDaily = virtDaily  - realDaily;
        float dAnn   = virtAnnual - realAnnual;
        float dRev   = virtRev    - realRev;

        // 테이블 업데이트
        SetText(realIrradianceText, $"{realIrr:F1}");
        SetText(virtIrradianceText, $"{virtIrr:F1}");
        SetDelta(dIrrText, dIrr, "F1");

        SetText(realDailyKwhText, $"{realDaily:F1}");
        SetText(virtDailyKwhText, $"{virtDaily:F1}");
        SetDelta(dDailyText, dDaily, "F1");

        SetText(realAnnualMwhText, $"{realAnnual / 1000f:F1}");
        SetText(virtAnnualMwhText, $"{virtAnnual / 1000f:F1}");
        SetDelta(dAnnualText, dAnn / 1000f, "F1");

        SetText(realAnnualRevText, FormatKRW(realRev));
        SetText(virtAnnualRevText, FormatKRW(virtRev));
        SetDeltaKRW(dRevText, dRev);

        // 비교 바 (실제 = 100%, 가상은 상대 비율)
        float ratio = realDaily > 0.001f ? Mathf.Clamp01(virtDaily / realDaily) : 0f;
        if (realBarFill) SetBarFill(realBarFill, 100f);
        if (virtBarFill) SetBarFill(virtBarFill, Mathf.Max(4f, ratio * 100f));
    }

    // ── 텍스트 헬퍼 ──────────────────────────────────

    private void SetText(TMP_Text t, string s) { if (t) t.text = s; }

    private void SetDelta(TMP_Text t, float delta, string fmt)
    {
        if (!t) return;
        t.text  = (delta >= 0 ? "+" : "") + delta.ToString(fmt);
        t.color = delta >= 0 ? GreenColor : OrangeColor;
    }

    private void SetDeltaKRW(TMP_Text t, float delta)
    {
        if (!t) return;
        float abs  = Mathf.Abs(delta);
        string sign = delta >= 0 ? "+" : "−";
        string val  = abs >= 1e6f  ? $"{abs / 1e6f:F1}M원"
                    : abs >= 1000f ? $"{Mathf.RoundToInt(abs / 1000f)}천원"
                    :                $"{Mathf.RoundToInt(abs)}원";
        t.text  = $"{sign}{val}";
        t.color = delta >= 0 ? GreenColor : OrangeColor;
    }

    private static string FormatKRW(float krw)
    {
        float abs  = Mathf.Abs(krw);
        string sign = krw < 0 ? "−" : "";
        if (abs >= 1e6f)  return $"{sign}{abs / 1e6f:F1}M원";
        if (abs >= 1000f) return $"{sign}{Mathf.RoundToInt(abs / 1000f)}천원";
        return $"{sign}{Mathf.RoundToInt(abs)}원";
    }

    private static void SetBarFill(ProgressBar rt, float ratio)
    {
        if (!rt) return;
        rt.currentValue = Mathf.Clamp01(ratio) * 100f;
    }
}
