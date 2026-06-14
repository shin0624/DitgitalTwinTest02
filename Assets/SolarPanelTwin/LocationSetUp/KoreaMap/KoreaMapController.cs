using UnityEngine;
using UnityEngine.EventSystems;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;

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

    public bool HasLocation {get; private set;} = false; // 외부(PanelPlacementManager)에서 읽을 수 있도록 퍼블릭 프로퍼티로 공개


    void Update()
    {
        if(EventSystem.current.IsPointerOverGameObject())// UI 위에서의 클릭은 무시
        {
            return;
        }

        if(Input.GetMouseButtonDown(0))// 마우스 왼쪽 버튼 클릭 감지
        {
            HandleMapClick();
        }
    }

    private void HandleMapClick() // 마우스 클릭을 처리하는 함수
    {
        Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);// 마우스 클릭 위치에서 레이 생성

        if(!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            return; // 레이가 groundLayer에 닿지 않으면 함수 종료
        }

        // Unity 월드좌표를 위경도로 변환 (hit.point는 CesiumGeoreference 기준의 Unity 좌표)
        double3 ecef = georeference.TransformUnityPositionToEarthCenteredEarthFixed(new double3(hit.point.x, hit.point.y, hit.point.z)); // ECEF 좌표로 변환

        double3 lonlatHeight = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(ecef);// 위경도와 고도로 변환 (lonlatHeight.x = 경도, lonlatHeight.y = 위도, lonlatHeight.z = 고도)

        double lon = lonlatHeight.x;
        double lat = lonlatHeight.y;

        // 한국 영역의 범위를 체크(위도 33~39, 경도 124~132)
        if(lat < 33.0 || lat > 39.0 || lon <124.0 || lon > 132.0)
        {
            Debug.LogWarning("[MapController] 한국 영역 밖은 클릭을 무시합니다.");
            return;
        }

        siteConfig.latitude = lat; // SiteConfigSO에 위도와 경도 저장
        siteConfig.longitude = lon; 

        var (nx, ny) = GridCoordConverter.LatLonToGrid(lat, lon);// 위경도를 격자 좌표로 변환
        Debug.Log($"[MapController] 선택 위치 → 위도:{lat:F4} 경도:{lon:F4}  격자:nx={nx}, ny={ny}");

        HasLocation = true; // 위치가 설정되었음을 나타내는 플래그를 true로 설정

        if(locationInfoText != null)
        {
            locationInfoText.text = $"위도 {lat:F4}°  경도 {lon:F4}°\n기상청 격자 nx={nx}, ny={ny}";
        }

    }
}
