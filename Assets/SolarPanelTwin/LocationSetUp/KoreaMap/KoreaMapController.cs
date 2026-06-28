using UnityEngine;
using UnityEngine.EventSystems;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;
using UnityEngine.UI;

public class KoreaMapController : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO siteConfig;

    [Header("Cesium 참조")]
    [SerializeField] private CesiumGeoreference georeference;//씬의 CesiumGeoreference 참조

    [Header("클릭 감지 설정")]
    [SerializeField] private LayerMask groundLayer;// KoreaGroundPlane이 속한 레이어

    [SerializeField] private Camera mapCamera; // 지도를 보는 카메라

    [Header("UI 출력")]
    [SerializeField] TextMeshProUGUI locationInfoText; // 위치 정보를 표시할 UI 텍스트

    [Header("MapArea RawImage")]
    [SerializeField] private RawImage mapRawImage; // MapArea의 RawImage 컴포넌트 참조

    public bool HasLocation {get; private set;} = false; // 외부(PanelPlacementManager)에서 읽을 수 있도록 퍼블릭 프로퍼티로 공개
    public int SelectionVersion { get; private set; } = 0;

    private Camera _uiEventCamera;

    private void Awake()
    {
        if (mapRawImage != null)
        {
            Canvas canvas = mapRawImage.canvas;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiEventCamera = canvas.worldCamera;
            }
        }
    }


    void Update()
    {
        if(Input.GetMouseButtonDown(0))// 마우스 왼쪽 버튼 클릭 감지
        {
            Vector2 mousePos = Input.mousePosition;

            // MapArea RawImage 안에서만 클릭 처리
            if (!IsPointerOverMapArea(mousePos))
            {
                return;
            }

            HandleMapClick(mousePos);
        }
    }

    private bool IsPointerOverMapArea(Vector2 screenPos)
    {
        if (mapRawImage == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            mapRawImage.rectTransform,
            screenPos,
            _uiEventCamera);
    }

    private void HandleMapClick(Vector2 screenPos) // 마우스 클릭을 처리하는 함수
    {
        if (siteConfig == null || georeference == null || mapCamera == null || mapRawImage == null)
        {
            Debug.LogWarning("[MapController] 필수 참조가 비어 있어 클릭 처리를 중단합니다.");
            return;
        }

        // RawImage 영역 안 클릭인지 먼저 확인
        RectTransform rt = mapRawImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPos, _uiEventCamera, out Vector2 localPoint))
            return;

        // RawImage 영역 내 UV 좌표 계산 (0~1)
        Rect rect = rt.rect;
        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        if (u < 0f || u > 1f || v < 0f || v > 1f) return; // 영역 밖이면 무시

        // RawImage UV(0~1)를 mapCamera viewport로 변환 후 레이캐스트
        Vector3 viewportPoint = new Vector3(u, v, 0f);
        Ray ray = mapCamera.ViewportPointToRay(viewportPoint); // ScreenPoint 대신 ViewportPoint 사용

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Debug.LogWarning("[MapController] 지면 레이캐스트 실패. groundLayer/콜라이더를 확인하세요.");
            return;
        }

        //ECEF 변환 → 위경도 저장
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

        siteConfig.latitude  = lat;
        siteConfig.longitude = lon;
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
