using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CesiumForUnity;
using TMPro;

public class PanelPlacementManager : MonoBehaviour
{   
    // UI 슬라이더로 태양광 패널 파라미터를 조정하고, SiteConfigSO에 업데이트 -> Prefab 배치를 담당하는 매니저 스크립트

    [Header("SO 참조")]
    [SerializeField] private SiteConfigSO siteConfig;

    [Header("의존 컴포넌트")]
    [SerializeField] private KoreaMapController mapController;
    [SerializeField] private CesiumGeoreference georeference;

    [Header("패널 프리팹")]
    [SerializeField] private GameObject solarPanelPrefab;

    [Header("UI - 슬라이더")]
    [SerializeField] private Slider tiltSlider;//기울기 0~90
    [SerializeField] private Slider azimuthSlider;//방위각 0~360
    [SerializeField] private Slider panelCountSlider; // 패널 수 1~50

    [Header("UI - 레이블")]
    [SerializeField] private TMP_Text tiltLabel;
    [SerializeField] private TMP_Text azimuthLabel;
    [SerializeField] private TMP_Text panelCountLabel;

    [Header("UI - 버튼")]
    [SerializeField] private Button confirmButton; // "이 위치로 확정" 버튼

    [Header("씬 전환")]
    [SerializeField] private string nextSceneName = "SolarPanelTwin";

    private GameObject _placedPanelGroup; // 배치된 패널들을 담는 부모 오브젝트

    void Start()
    {
        // 슬라이더 초기값을 SiteConfigSO 기본값으로 설정
        tiltSlider.value = siteConfig.tiltAngleDeg;
        azimuthSlider.value = siteConfig.azimuthDeg;
        panelCountSlider.value = siteConfig.panelCount;

        // 슬라이더 이벤트 연결
        tiltSlider.onValueChanged.AddListener(OnTiltChanged);
        azimuthSlider.onValueChanged.AddListener(OnAzimuthChanged);
        panelCountSlider.onValueChanged.AddListener(OnPanelCountChanged);

        // 확정 버튼 연결
        confirmButton.onClick.AddListener(OnConfirm);
        confirmButton.interactable = false; // 위치 선택 전까지 비활성

        UpdateLabels();
    }

    void Update()
    {
        if(mapController.HasLocation && _placedPanelGroup == null) // 지도에서 위치가 선택되면 패널 즉시 배치 + 확정 버튼 활성화
        {
            PlacePanels();
            confirmButton.interactable = true; // 패널이 배치되면 확정 버튼 활성화
        }

        if(_placedPanelGroup !=null)// 파라미터 변경 시 패널 위치와 방향 갱신
        {
            UpdatePanelRotation();
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
            Destroy(_placedPanelGroup);// 기존 패널 그룹 삭제
            _placedPanelGroup = null;// 참조 초기화
            PlacePanels(); // 변경된 패널 수로 새롭게 배치
        }
    }

    // 패널 배치 메서드

    private void PlacePanels()
    {
        _placedPanelGroup = new GameObject("PlacePanelGroup");// 패널들을 담을 부모 오브젝트 생성

        int count = siteConfig.panelCount;// 배치할 패널 수
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));// 패널을 배치할 열 수 계산(정방형 배열로 배치하여 공간 효율 극대화)

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;// 현재 패널의 행 인덱스
            int col = i % cols;// 현재 패널의 열 인덱스

           
            GameObject panel = Instantiate(solarPanelPrefab, _placedPanelGroup.transform);// 패널 프리팹을 인스턴스화하여 부모 오브젝트에 배치

            var anchor = panel.AddComponent<CesiumGlobeAnchor>(); //패널을 위경도 기준으로 지구에 고정하기 위해 CesiuumGlobeAnchor 사용
            anchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(
                siteConfig.longitude + col * 0.00002,
                siteConfig.latitude - row * 0.00002,
                10.0
            ); // 패널 간 약 2m 간격으로 배치하며, 지면에서 약 10m 높이에 위치하도록 설정(조정 가능하도록)

            UpdatePanelRotation(panel);// 패널의 회전을 업데이트하여 초기 방위각과 기울기를 적용
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
        //방위각 : Y축 회전(남향 : 180도)
        // 기울기 : X축 회전
        panel.transform.rotation = Quaternion.Euler(siteConfig.tiltAngleDeg, siteConfig.azimuthDeg, 0.0f);// 패널의 로컬 회전을 설정하여 방위각과 기울기를 적용
    }

    // 위치 확정 버튼 메서드

    private void OnConfirm() // 위치 확정 버튼 클릭 시 호출되는 메서드
    {
        siteConfig.siteConfirmed = true; // 위치 설정이 완료되었음을 나타내는 플래그를 true로 설정하여 다음 단계로 진행할 수 있도록 함

        // RealWeatherFetcher는 씬에 하나만 존재하므로, 자동으로 Start()에서 폴링 시작
        var fetcher = FindAnyObjectByType<RealWeatherFetcher>();// fetcher는 이미 Start()에서 코루틴이 실행 중일 것이므로, 추가 호출 불필요

        SceneManager.LoadScene(nextSceneName);
    }

    // UI 레이블 업데이트 메서드
    private void UpdateLabels() // 슬라이더 값에 따라 레이블 텍스트를 업데이트
    {
        if(tiltLabel)
        {
            tiltLabel.text = $"기울기 : {siteConfig.tiltAngleDeg:F0}°";
        }
        if (azimuthLabel)
        {
            azimuthLabel.text  = $"방위각: {siteConfig.azimuthDeg:F0}°";
        } 
        if (panelCountLabel)
        {
            panelCountLabel.text = $"패널 수: {siteConfig.panelCount}개";
        }
    }
}
