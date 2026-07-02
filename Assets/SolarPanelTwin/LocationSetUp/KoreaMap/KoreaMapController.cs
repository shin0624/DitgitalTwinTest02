using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;
using UnityEngine.UI;
using Michsky.UI.Heat;

public class KoreaMapController : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO siteConfig;

    [Header("Cesium 참조")]
    [SerializeField] private CesiumGeoreference georeference;

    [Header("클릭 감지 설정")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Camera mapCamera;

    [Header("UI 출력")]
    [SerializeField] private TextMeshProUGUI locationInfoText;

    [Header("MapArea RawImage")]
    [SerializeField] private RawImage mapRawImage;

    [Header("설치 확인 패널")]
    [SerializeField] private ModalWindowManager confirmModal;
    [SerializeField] private TMP_Text confirmQuestionText;

    [Header("역지오코딩")]
    [Tooltip("Node.js 프록시의 역지오코딩 엔드포인트.\n예) http://localhost:3000/reverse-geocode")]
    [SerializeField] private string reverseGeocodeUrl = "http://localhost:3000/reverse-geocode";

    public bool HasLocation { get; private set; } = false;
    public int SelectionVersion { get; private set; } = 0;

    private Camera _uiEventCamera;
    private double _pendingLat, _pendingLon, _pendingHeight;

    private void Awake()
    {
        if (mapRawImage != null)
        {
            Canvas canvas = mapRawImage.canvas;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiEventCamera = canvas.worldCamera;
        }

        if (confirmModal != null)
            confirmModal.onConfirm.AddListener(OnConfirmYes);
    }

    void Update()
    {
        if (confirmModal != null && confirmModal.isOn) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (!IsPointerOverMapArea(mousePos)) return;
            HandleMapClick(mousePos);
        }
    }

    private bool IsPointerOverMapArea(Vector2 screenPos)
    {
        if (mapRawImage == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            mapRawImage.rectTransform, screenPos, _uiEventCamera);
    }

    private void HandleMapClick(Vector2 screenPos)
    {
        if (siteConfig == null || georeference == null || mapCamera == null || mapRawImage == null)
        {
            Debug.LogWarning("[MapController] 필수 참조가 비어 있어 클릭 처리를 중단합니다.");
            return;
        }

        RectTransform rt = mapRawImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, _uiEventCamera, out Vector2 localPoint)) return;

        Rect rect = rt.rect;
        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        Ray ray = mapCamera.ViewportPointToRay(new Vector3(u, v, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Debug.LogWarning("[MapController] 지면 레이캐스트 실패.");
            return;
        }

        double3 ecef = georeference.TransformUnityPositionToEarthCenteredEarthFixed(
            new double3(hit.point.x, hit.point.y, hit.point.z));
        double3 lonlatHeight = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(ecef);

        double lon = lonlatHeight.x;
        double lat = lonlatHeight.y;
        double height = lonlatHeight.z;

        if (lat < 33.0 || lat > 39.0 || lon < 124.0 || lon > 132.0)
        {
            Debug.LogWarning("[MapController] 한국 영역 밖 클릭 무시");
            return;
        }

        _pendingLat    = lat;
        _pendingLon    = lon;
        _pendingHeight = height;

        if (confirmModal != null)
        {
            SetQuestion($"{lat:F4}°N,  {lon:F4}°E", isLoading: true);
            confirmModal.OpenWindow();
            StopAllCoroutines();
            StartCoroutine(FetchAddress(lat, lon));
        }
        else
        {
            CommitLocation(lat, lon, height);
        }
    }

    // ── 역지오코딩 ────────────────────────────────────────────

    private IEnumerator FetchAddress(double lat, double lon)
    {
        if (string.IsNullOrWhiteSpace(reverseGeocodeUrl)) yield break;

        string url = $"{reverseGeocodeUrl.TrimEnd('/')}?lat={lat:F6}&lon={lon:F6}";
        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = 5;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string address = GetJsonString(req.downloadHandler.text, "address");
            if (!string.IsNullOrEmpty(address))
            {
                SetQuestion(address, isLoading: false);
                yield break;
            }
        }

        SetQuestion($"{lat:F4}°N,  {lon:F4}°E", isLoading: false);
    }

    private void SetQuestion(string locationStr, bool isLoading)
    {
        if (confirmQuestionText == null) return;
        string loading = isLoading ? " (조회 중...)" : "";
        confirmQuestionText.text =
            $"이 위치에 패널을 설치하시겠습니까?\n현재 위치 : {locationStr}{loading}";
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

        int start = i + 1, end = start;
        while (end < json.Length)
        {
            if (json[end] == '"' && json[end - 1] != '\\') break;
            end++;
        }
        return end >= json.Length ? null : json[start..end];
    }

    // ── 확인/취소 콜백 ───────────────────────────────────────

    private void OnConfirmYes() => CommitLocation(_pendingLat, _pendingLon, _pendingHeight);

    private void CommitLocation(double lat, double lon, double height)
    {
        siteConfig.latitude     = lat;
        siteConfig.longitude    = lon;
        siteConfig.heightMeters = height;

        var (nx, ny) = GridCoordConverter.LatLonToGrid(lat, lon);
        siteConfig.nx = nx;
        siteConfig.ny = ny;
        Debug.Log($"[MapController] 선택 위치 → 위도:{lat:F4} 경도:{lon:F4}  격자:nx={nx}, ny={ny}");
        HasLocation = true;
        SelectionVersion++;

        if (locationInfoText != null)
            locationInfoText.text = $"위도 {lat:F4}°  경도 {lon:F4}°  고도 {height:F1}m\n기상청 격자 nx={nx}, ny={ny}";
    }
}
