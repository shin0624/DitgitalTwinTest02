# Unity 6 + Cesium for Unity 기반 태양광 설비 디지털트윈 및 AI 기반 ESS(에너지 저장 시스템) 솔루션 개발
## 사용 기술
- Unity 6 
- MQTT
- WebSocket
- Cesium for Unity
- Unity Sentis
- Pytorch
## (개발 전 1차 테스트) 온도센서 개발
- 가상 온도센서 개발 및 MQTT <-> Unity 통신 구현
https://www.notion.so/Digital-Twin-Test-362df6824ea58097a880efafba4f9db1?source=copy_link

## 태양광 설비 가시화 솔루션 개발
- 태양광 패널의 설치 위치 및 고도/갯수/방향 등 환경조건을 Unity WebGL 대시보드에서 설정 후, Cesium for Unity 플러그인을 사용하여 구현된 대한민국 지형 필드에 설치하는 시뮬레이션 시스템 개발
- 설치 위치/조건 등을 실시간으로 변경하며, 해당 환경조건/제약조건에 따른 발전량/향후 수익 등을 AI 기반으로 시뮬레이션 가능
- 설치 위치가 확정되면, 기상청 API 기반 해당 위치의 실시간/1일 단위 환경조건(기후 등) 변화에 따른 패널 온도/발전효율/상태 모니터링 등을 지원하는 대시보드 접근 가능
- 태양광 패널 센서와 발전량 등 시계열 데이터는 실시간 수집되며, ESS(에너지 저장 시스템)에 ONNX포맷으로 내장된 시계열 예측 AI 모델의 실시간 추론을 통해 향후 발전량, 수익, 효율 등의 지표 산출이 가능
<img width="1705" height="960" alt="Image" src="https://github.com/user-attachments/assets/49bf74c6-33ce-46fe-9c34-61e10dca3acc" />
