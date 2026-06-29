using UnityEngine;
using UnityEngine.SceneManagement;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine.UI;

public class PanelPlacementManager : MonoBehaviour
{   
    // UI 슬라이더로 태양광 패널 파라미터를 조정하고, SiteConfigSO에 업데이트 -> Prefab 배치를 담당하는 매니저 스크립트

    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO siteConfig;

    [Header("의존 컴포넌트")]
    [SerializeField] private KoreaMapController mapController;
    [SerializeField] private CesiumGeoreference georeference;
    [SerializeField] private Camera mapCamera;

    [Header("패널 프리팹")]
    [SerializeField] private GameObject solarPanelPrefab;

    [Header("UI - 슬라이더")]
    [SerializeField] private SliderManager tiltSlider;//기울기 0~90
    [SerializeField] private SliderManager azimuthSlider;//방위각 0~360
    [SerializeField] private SliderManager panelCountSlider; // 패널 수 1~50

    [Header("UI - 레이블")]
    [SerializeField] private TMP_Text tiltLabel;
    [SerializeField] private TMP_Text azimuthLabel;
    [SerializeField] private TMP_Text panelCountLabel;

    [Header("UI - 버튼")]
    [SerializeField] private Button confirmButton; // "이 위치로 확정" 버튼

    [Header("씬 전환")]
    [SerializeField] private string nextSceneName = "SolarPanelTwin";

    [Header("배치 간격")]
    [SerializeField] private float panelSpacingMeters = 2.0f;

    [Header("지형 접촉 보정")]
    [SerializeField] private float terrainContactMarginMeters = 0.02f;
    [SerializeField] private LayerMask terrainLayerMask = ~0;
    [SerializeField] private float terrainSampleRayHeightMeters = 2000.0f;
    [SerializeField] private float terrainSampleRayDistanceMeters = 5000.0f;

    [Header("카메라 포커스")]
    [SerializeField] private bool lockDynamicCameraOnPlacement = true;
    [SerializeField] private Vector3 cameraOffsetFromPanel = new Vector3(0.0f, 4.0f, -8.0f);
    [SerializeField] private float panelFrontLookDistanceMeters = 0.5f;

    [Header("카메라 궤도 회전 (우클릭 드래그)")]
    [SerializeField] private float orbitSensitivity = 2.0f;
    [SerializeField] private float minOrbitPitch = -80f;
    [SerializeField] private float maxOrbitPitch = 80f;

    private GameObject _placedPanelGroup; // 배치된 패널들을 담는 부모 오브젝트
    private int _lastSelectionVersion = -1;
    private CesiumCameraController _dynamicCameraController;
    private bool _isCameraLocked;
    private bool _prevEnableMovement;
    private bool _prevEnableRotation;
    private Transform _focusedPanel;
    private readonly Dictionary<CesiumGlobeAnchor, double> _panelTerrainHeightMap = new Dictionary<CesiumGlobeAnchor, double>();

    private float _orbitYaw;
    private float _orbitPitch;
    private float _orbitDistance;
    private bool _isOrbiting;

    void Start()
    {
        if (mapCamera == null)
        {
            mapCamera = Camera.main;
        }

        if (mapCamera != null)
        {
            _dynamicCameraController = mapCamera.GetComponent<CesiumCameraController>();
        }

        // 슬라이더 초기값을 SiteConfigSO 기본값으로 설정
        if (tiltSlider != null && tiltSlider.mainSlider != null)
        {
            tiltSlider.mainSlider.value = siteConfig.tiltAngleDeg;
            tiltSlider.UpdateUI();
        }
        if (azimuthSlider != null && azimuthSlider.mainSlider != null)
        {
            azimuthSlider.mainSlider.value = siteConfig.azimuthDeg;
            azimuthSlider.UpdateUI();
        }
        if (panelCountSlider != null && panelCountSlider.mainSlider != null)
        {
            panelCountSlider.mainSlider.value = siteConfig.panelCount;
            panelCountSlider.UpdateUI();
        }

        // 슬라이더 이벤트 연결
        if (tiltSlider != null)
        {
            tiltSlider.onValueChanged.AddListener(OnTiltChanged);
        }
        if (azimuthSlider != null)
        {
            azimuthSlider.onValueChanged.AddListener(OnAzimuthChanged);
        }
        if (panelCountSlider != null)
        {
            panelCountSlider.onValueChanged.AddListener(OnPanelCountChanged);
        }

        // 확정 버튼 연결
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
            confirmButton.interactable = false; // 위치 선택 전까지 비활성
        }

        UpdateLabels();
    }

    void Update()
    {
        if (_isCameraLocked && Input.GetKeyDown(KeyCode.Escape))
        {
            ReleaseCameraLock();
        }

        if (mapController != null && mapController.HasLocation && mapController.SelectionVersion != _lastSelectionVersion)
        {
            RebuildPanels();
            _lastSelectionVersion = mapController.SelectionVersion;

            if (_placedPanelGroup != null)
            {
                if (confirmButton != null)
                {
                    confirmButton.interactable = true; // 패널이 배치되면 확정 버튼 활성화
                }
                FocusCameraOnPrimaryPanel();
            }
        }

        if(_placedPanelGroup !=null)// 파라미터 변경 시 패널 위치와 방향 갱신
        {
            UpdatePanelRotation();

            if (_isCameraLocked && _focusedPanel != null && mapCamera != null)
            {
                HandleOrbitInput();
                ApplyOrbitCamera();
            }
        }
    }

    // 슬라이더 콜백 메서드
    private void OnTiltChanged(float value)
    {
        siteConfig.tiltAngleDeg = value;
        UpdateLabels();
    }

    private void OnAzimuthChanged(float value)
    {
        siteConfig.azimuthDeg = value;
        UpdateLabels();
    }

    private void OnPanelCountChanged(float value)
    {
        siteConfig.panelCount = Mathf.RoundToInt(value);// 패널 수는 정수여야 하므로 반올림
        UpdateLabels();

        if(_placedPanelGroup != null)
        {
            RebuildPanels(); // 변경된 패널 수로 새롭게 배치
            FocusCameraOnPrimaryPanel();
        }
    }

    private void RebuildPanels()
    {
        _panelTerrainHeightMap.Clear();

        if (_placedPanelGroup != null)
        {
            Destroy(_placedPanelGroup);// 기존 패널 그룹 삭제
            _placedPanelGroup = null;// 참조 초기화
        }

        PlacePanels();
    }

    // 패널 배치 메서드

    private void PlacePanels()
    {
        if (georeference == null)
        {
            Debug.LogError("[PanelPlacementManager] CesiumGeoreference 참조가 없어 패널을 배치할 수 없습니다.");
            return;
        }

        _placedPanelGroup = new GameObject("PlacePanelGroup");// 패널들을 담을 부모 오브젝트 생성
        _placedPanelGroup.transform.SetParent(georeference.transform, false);

        int count = siteConfig.panelCount;// 배치할 패널 수
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));// 패널을 배치할 열 수 계산(정방형 배열로 배치하여 공간 효율 극대화)
        double baseLon = siteConfig.longitude;
        double baseLat = siteConfig.latitude;
        double baseHeight = siteConfig.heightMeters;

        double degPerMeterLat = 1.0 / 111320.0;
        double cosLat = Mathf.Cos((float)(baseLat * Mathf.Deg2Rad));
        double degPerMeterLon = (cosLat > 0.0001f) ? 1.0 / (111320.0 * cosLat) : 0.0;

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;// 현재 패널의 행 인덱스
            int col = i % cols;// 현재 패널의 열 인덱스

            double eastMeters = col * panelSpacingMeters;
            double southMeters = row * panelSpacingMeters;

            double panelLon = baseLon + eastMeters * degPerMeterLon;
            double panelLat = baseLat - southMeters * degPerMeterLat;

           
            GameObject panel = Instantiate(solarPanelPrefab, _placedPanelGroup.transform);// 패널 프리팹을 인스턴스화하여 부모 오브젝트에 배치

            var anchor = panel.GetComponent<CesiumGlobeAnchor>();
            if (anchor == null)
            {
                anchor = panel.AddComponent<CesiumGlobeAnchor>(); //패널을 위경도 기준으로 지구에 고정하기 위해 CesiuumGlobeAnchor 사용
            }
            // 패널 방향은 슬라이더(기울기/방위각)로 직접 제어하므로, 지구 곡률 자동 보정 회전은 끈다.
            anchor.adjustOrientationForGlobeWhenMoving = false;
            anchor.detectTransformChanges = false;

            anchor.longitudeLatitudeHeight = new double3(
                panelLon,
                panelLat,
                baseHeight
            );

            UpdatePanelRotation(panel);// 패널의 회전을 업데이트하여 초기 방위각과 기울기를 적용
            _panelTerrainHeightMap[anchor] = baseHeight;
            FitPanelBottomToTerrain(anchor, panel, baseHeight);
        }
    }

    private void UpdatePanelRotation()// 모든 패널의 회전을 업데이트하는 메서드
    {
        if(_placedPanelGroup == null)
        {
            return;
        }

        foreach(Transform child in _placedPanelGroup.transform)// 패널 그룹의 모든 자식(패널) 순회
        {
            UpdatePanelRotation(child.gameObject);// 각 패널의 회전 업데이트
        }
    }

    private void UpdatePanelRotation(GameObject panel) // 단일 패널의 회전을 업데이트하는 메서드
    {
        // 방위각(Y)과 기울기(X)를 ENU 좌표계 기준 회전으로 적용
        CesiumGlobeAnchor panelAnchor = null;
        if (panel.TryGetComponent(out panelAnchor))
        {
            quaternion enuRotation = quaternion.EulerXYZ(
                math.radians(siteConfig.tiltAngleDeg),
                math.radians(siteConfig.azimuthDeg),
                0.0f);
            panelAnchor.rotationEastUpNorth = enuRotation;
        }
        else
        {
            panel.transform.rotation = Quaternion.Euler(siteConfig.tiltAngleDeg, siteConfig.azimuthDeg, 0.0f);
        }

        if (panelAnchor != null && _panelTerrainHeightMap.TryGetValue(panelAnchor, out double terrainHeight))
        {
            FitPanelBottomToTerrain(panelAnchor, panel, terrainHeight);
        }
    }

    private void FitPanelBottomToTerrain(CesiumGlobeAnchor anchor, GameObject panel, double terrainHeight)
    {
        if (TrySampleTerrainHeightMeters(anchor, out double sampledTerrainHeight))
        {
            terrainHeight = sampledTerrainHeight;
            _panelTerrainHeightMap[anchor] = sampledTerrainHeight;
        }

        float lowestEnuY = GetLowestEnuY(panel.transform, anchor.rotationEastUpNorth);
        double liftMeters = (lowestEnuY < 0.0f) ? -lowestEnuY : 0.0;

        double3 llh = anchor.longitudeLatitudeHeight;
        llh.z = terrainHeight + liftMeters + terrainContactMarginMeters;
        anchor.longitudeLatitudeHeight = llh;
    }

    private bool TrySampleTerrainHeightMeters(CesiumGlobeAnchor anchor, out double terrainHeight)
    {
        terrainHeight = 0.0;
        if (georeference == null)
        {
            return false;
        }

        double3 llh = anchor.longitudeLatitudeHeight;
        double3 sampleLlh = new double3(llh.x, llh.y, llh.z + terrainSampleRayHeightMeters);
        double3 sampleEcef = georeference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(sampleLlh);
        double3 sampleUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(sampleEcef);

        Vector3 rayOrigin = new Vector3((float)sampleUnity.x, (float)sampleUnity.y, (float)sampleUnity.z);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, terrainSampleRayDistanceMeters, terrainLayerMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit? validHit = null;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            if (anchor.transform != null && hitTransform.IsChildOf(anchor.transform.root))
            {
                continue;
            }

            if (_placedPanelGroup != null && hitTransform.IsChildOf(_placedPanelGroup.transform))
            {
                continue;
            }

            validHit = hits[i];
            break;
        }

        if (!validHit.HasValue)
        {
            return false;
        }

        Vector3 point = validHit.Value.point;
        double3 hitEcef = georeference.TransformUnityPositionToEarthCenteredEarthFixed(new double3(point.x, point.y, point.z));
        double3 hitLlh = georeference.ellipsoid.CenteredFixedToLongitudeLatitudeHeight(hitEcef);
        terrainHeight = hitLlh.z;
        return true;
    }

    private float GetLowestEnuY(Transform root, quaternion enuRotation)
    {
        float minEnuY = float.PositiveInfinity;
        Matrix4x4 worldToRoot = root.worldToLocalMatrix;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            Matrix4x4 localToRoot = worldToRoot * meshFilter.transform.localToWorldMatrix;
            Bounds bounds = meshFilter.sharedMesh.bounds;
            Vector3[] corners = GetBoundsCorners(bounds);

            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localPoint = localToRoot.MultiplyPoint3x4(corners[c]);
                float3 enuPoint = math.mul(enuRotation, new float3(localPoint.x, localPoint.y, localPoint.z));
                if (enuPoint.y < minEnuY)
                {
                    minEnuY = enuPoint.y;
                }
            }
        }

        if (!float.IsFinite(minEnuY))
        {
            return 0.0f;
        }

        return minEnuY;
    }

    private Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        return new Vector3[]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };
    }

    // 위치 확정 버튼 메서드

    private void OnConfirm() // 위치 확정 버튼 클릭 시 호출되는 메서드
    {
        siteConfig.siteConfirmed = true; // 위치 설정이 완료되었음을 나타내는 플래그를 true로 설정하여 다음 단계로 진행할 수 있도록 함

        // RealWeatherFetcher는 씬에 하나만 존재하므로, 자동으로 Start()에서 폴링 시작
        var fetcher = FindAnyObjectByType<RealWeatherFetcher>();// fetcher는 이미 Start()에서 코루틴이 실행 중일 것이므로, 추가 호출 불필요

        SceneManager.LoadScene(nextSceneName);
    }

    private void FocusCameraOnPrimaryPanel()
    {
        if (!lockDynamicCameraOnPlacement || mapCamera == null || _placedPanelGroup == null || _placedPanelGroup.transform.childCount == 0)
        {
            return;
        }

        Transform panel = _placedPanelGroup.transform.GetChild(0);
        _focusedPanel = panel;

        Vector3 worldOffset = panel.TransformDirection(cameraOffsetFromPanel);
        Vector3 lookTarget = GetPanelFrontLookTarget(panel);
        mapCamera.transform.position = panel.position + worldOffset;
        mapCamera.transform.LookAt(lookTarget, Vector3.up);

        // 궤도 파라미터 초기화
        Vector3 toCam = mapCamera.transform.position - panel.position;
        _orbitDistance = toCam.magnitude;
        if (_orbitDistance < 0.001f) _orbitDistance = cameraOffsetFromPanel.magnitude;
        Vector3 camDir = toCam.normalized;
        _orbitPitch = Mathf.Asin(Mathf.Clamp(camDir.y, -1f, 1f)) * Mathf.Rad2Deg;
        _orbitYaw = Mathf.Atan2(camDir.x, camDir.z) * Mathf.Rad2Deg;
        _isOrbiting = false;

        if (_dynamicCameraController == null)
        {
            _dynamicCameraController = mapCamera.GetComponent<CesiumCameraController>();
        }

        if (_dynamicCameraController == null)
        {
            return;
        }

        if (!_isCameraLocked)
        {
            _prevEnableMovement = _dynamicCameraController.enableMovement;
            _prevEnableRotation = _dynamicCameraController.enableRotation;
        }

        _dynamicCameraController.enableMovement = false;
        _dynamicCameraController.enableRotation = false;
        _isCameraLocked = true;
    }

    private void ReleaseCameraLock()
    {
        if (_dynamicCameraController != null)
        {
            _dynamicCameraController.enableMovement = _prevEnableMovement;
            _dynamicCameraController.enableRotation = _prevEnableRotation;
        }

        _isCameraLocked = false;
        _isOrbiting = false;
        _focusedPanel = null;
    }

    private void HandleOrbitInput()
    {
        if (Input.GetMouseButtonDown(1)) _isOrbiting = true;
        if (Input.GetMouseButtonUp(1))   _isOrbiting = false;

        if (!_isOrbiting) return;

        _orbitYaw   += Input.GetAxis("Mouse X") * orbitSensitivity;
        _orbitPitch  = Mathf.Clamp(
            _orbitPitch - Input.GetAxis("Mouse Y") * orbitSensitivity,
            minOrbitPitch, maxOrbitPitch);
    }

    private void ApplyOrbitCamera()
    {
        float pitchRad = _orbitPitch * Mathf.Deg2Rad;
        float yawRad   = _orbitYaw   * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            Mathf.Sin(pitchRad),
            Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        ) * _orbitDistance;

        mapCamera.transform.position = _focusedPanel.position + offset;
        mapCamera.transform.LookAt(_focusedPanel.position, Vector3.up);
    }

    private Vector3 GetPanelFrontLookTarget(Transform panel)
    {
        // 패널 프리팹의 정면을 -Z로 정의했으므로, 월드 기준 정면 벡터는 TransformDirection(Vector3.back)이다.
        Vector3 panelFront = panel.TransformDirection(Vector3.back);
        return panel.position + panelFront * panelFrontLookDistanceMeters;
    }

    // UI 레이블 업데이트 메서드
    private void UpdateLabels() // 슬라이더 값에 따라 레이블 텍스트를 업데이트
    {
        if(tiltLabel)
        {
            tiltLabel.text = $"{siteConfig.tiltAngleDeg:F0}°";
        }
        if (azimuthLabel)
        {
            azimuthLabel.text  = $"{siteConfig.azimuthDeg:F0}°";
        } 
        if (panelCountLabel)
        {
            panelCountLabel.text = $"{siteConfig.panelCount}개";
        }
    }
}
