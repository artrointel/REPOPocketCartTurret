# 개발 환경 검증

- Debug/Release 빌드가 성공하는지 확인한다.
- Debug 빌드 후 개발 프로필의 `BepInEx/plugins/MiniTurret/`에 DLL/PDB가 생성되는지 확인한다.
- `MiniTurret: Start game`으로 게임을 실행하고 BepInEx 로그에서 `MiniTurret v0.1.0 loaded.`를 확인한다.
- `F5`로 Mono 디버거가 연결되는지 확인한다. 초기 로딩 이후 재현 가능한 코드에 중단점을 설정한다.
- 로그 감시 작업이 선택한 프로필의 로그를 표시하는지 확인한다.
- 패키지 ZIP에 manifest, README, CHANGELOG, 256x256 PNG, `BepInEx/plugins/MiniTurret/MiniTurret.dll`이 포함되는지 확인한다.

게임 실행과 디버거 연결은 실제 플레이 환경에서 별도로 확인해야 한다.
