# PocketCartTurret

**포켓카트에 캐넌이나 레이저를 장착해 자동 터렛으로 만드세요.**

가까운 적을 자동으로 탐지하고 추적해 발사합니다. 원하는 위치에 세워 무인 방어 터렛으로 활용하거나, 끌고 이동하면서 화력 지원을 받을 수 있습니다. 카트를 잡거나 밀고 있지 않아도 자동으로 동작합니다.

**호스트만 모드를 설치하면 됩니다. 친구들은 모드나 BepInEx를 설치하지 않은 순정 상태로도 함께 사용할 수 있습니다.** 회전·경고음·발사 이펙트·피해는 게임의 기존 멀티플레이 동기화를 사용합니다.

모드명은 **PocketCartTurret**, 저장소명은 **REPOPocketCartTurret**입니다. [GitHub 저장소](https://github.com/artrointel/REPOPocketCartTurret)

개발 환경은 REPORaiseToUpgrade와 같은 구성을 사용합니다.

## 동작

- 일반 미션 스테이지에서 미니카트에 장착된 대포·레이저만 자동 동작합니다. 대형 카트나 바닥에 놓인 무기는 대상이 아닙니다.
- 배터리가 남아 있을 때 대포는 17.5m, 레이저는 10.5m 이내의 적을 탐지합니다. 순정 최대 사거리 25m/15m의 70%입니다.
- 카트에서는 순정 기립 자세 유지 동작을 항상 보존합니다. 실제 총구를 좌우로 회전했을 때 사선이 닿는 적 중 가장 가까운 한 마리를 추적하며, 가상 기립 자세로 총구를 보정하거나 상하 조준하지 않습니다.
- 총구 방향이 실제로 적과 정렬되고 장애물이 없을 때 순정 준비·발사·재장전 절차를 시작합니다. 추적 중 사선이 막히거나 적이 사라지면 자동 준비·발사를 중단합니다.
- 배터리 소모와 피해, 사운드, 이펙트는 순정 무기 동작을 사용합니다. 미니카트를 끌고 다니는 조작은 유지됩니다.
- 플레이어가 무기 자체를 잡으면 자동 조준을 멈추고 순정 수동 조작을 허용합니다. 카트 손잡이를 잡아 끄는 것은 자동 동작을 멈추지 않습니다.
- 호스트가 탐지·회전을 결정하고 순정 RPC와 물리 위치 동기화를 사용합니다. 참가자는 모드를 설치하지 않아도 됩니다. 참가자에게만 모드가 있으면 자동 동작하지 않습니다.

게임 내 물리·장애물·순정 참가자 동기화에 대한 수동 검증 항목은 [planning/test-plan.md](planning/test-plan.md)에 정리되어 있습니다.


## Debug 전용 테스트 장비

Debug 빌드에서는 일반 미션 스테이지의 생성이 완료될 때 트럭 안전 스폰 지점 근처에 미니카트·대포·레이저를 각각 하나씩 추가 생성합니다. 무기는 분리된 상태로 생성되므로 직접 카트에 넣어 시험하세요. 상점·로비에서는 생성하지 않습니다. 구매 목록이나 보유 개수는 강제로 변경하지 않습니다.

스폰 소스는 `src/PocketCartTurret/Debug/`에 분리되어 있으며 `#if DEBUG`와 프로젝트의 컴파일 제외 규칙을 함께 적용했습니다. Release DLL에는 스폰 클래스와 스테이지 시작 패치가 포함되지 않습니다. Thunderstore 패키징은 Release 구성만 허용합니다.

## 개발 환경

- .NET 9 SDK, C# 12, `netstandard2.1`
- BepInEx 5.4.21, HarmonyX 2.7.0
- UnityEngine.Modules 2022.3.62
- R.E.P.O.GameLibs.Steam 0.4.4-ngd.0
- Linkoid.Repo.Plugin.Build 0.2.2

기준 프로젝트에서 실제 복원된 BepInEx 버전을 고정했습니다. 로컬 `.dotnet/` SDK와 `.nuget/packages/` 캐시는 Git에서 제외됩니다. 새 PC에서는 `global.json`에 맞는 .NET 9 SDK를 준비해야 합니다. VS Code 작업은 `.dotnet/dotnet.exe`를 사용합니다.

## 빌드 및 실행

현재 로컬 `Directory.Repo.props`는 Steam 기본 설치 경로와 Thunderstore Mod Manager의 `REPO` 프로필을 사용합니다. 빌드 시 DLL/PDB가 `BepInEx/plugins/PocketCartTurret/`에 배포됩니다.

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:NUGET_PACKAGES = "$PWD\.nuget\packages"
.\.dotnet\dotnet.exe restore src\PocketCartTurret\PocketCartTurret.csproj
.\.dotnet\dotnet.exe build src\PocketCartTurret\PocketCartTurret.csproj --configuration Debug --no-restore
```

이미 준비된 로컬 캐시만으로 복원하려면 다음 명령을 사용합니다.

```powershell
.\.dotnet\dotnet.exe restore src\PocketCartTurret\PocketCartTurret.csproj --source "$PWD\.nuget\packages" -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false
```

게임 프로필에 배포하지 않고 빌드만 확인하려면 `-p:CopyFilesToPluginOutputDirectoryOnBuild=false`를 빌드 명령에 추가합니다.

- `Ctrl+Shift+B`: Debug 빌드 및 개발 프로필에 배포
- `Tasks: Run Task` → `PocketCartTurret: Start game`: 빌드·배포 후 게임 실행
- `F5` → `PocketCartTurret: Launch and attach`: Mono 디버거로 실행 및 연결 (`127.0.0.1:10000`)
- `PocketCartTurret: Watch BepInEx log`: 개발 프로필 로그 확인

F5에는 VS Code의 C# Dev Kit과 Unity 확장이 필요합니다. 게임을 종료한 뒤 코드를 다시 빌드하고 실행하세요. 로딩 성공 시 `PocketCartTurret v0.1.0 loaded.`가 기록됩니다.

다른 프로필은 `Directory.Repo.props.example` 또는 `Directory.Repo.Gale.props.example`을 `Directory.Repo.props`로 복사해 지정할 수 있습니다. 실행 스크립트와 로그 작업의 경로도 함께 변경하세요. `Directory.Repo.props`는 개인 경로를 포함하므로 Git에서 제외됩니다.

## Thunderstore 패키지

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Pack-Thunderstore.ps1
```

결과는 `artifacts/PocketCartTurret-0.1.0.zip`에 생성됩니다. 패키징 빌드는 게임 프로필에 배포하지 않습니다. 아이콘 원본은 `assets/PocketCartTurret.png`, 제출용 256×256 PNG는 `thunderstore/icon.png`입니다. 버전은 csproj, `Plugin.cs`, `thunderstore/manifest.json` 세 곳에서 일치해야 합니다.

복원 완료 후 네트워크 없이 패키징하려면 `-NoRestore`를 추가합니다.

## 자동 검증

Debug와 Release를 모두 빌드한 뒤 다음 검사를 실행합니다. 게임 프로필 배포 없이 빌드하려면 앞에서 설명한 MSBuild 속성을 사용하세요.

```powershell
.\.dotnet\dotnet.exe restore tests\PocketCartTurret.Checks\PocketCartTurret.Checks.csproj --source "$PWD\.nuget\packages" -p:NuGetAudit=false
.\.dotnet\dotnet.exe run --project tests\PocketCartTurret.Checks\PocketCartTurret.Checks.csproj --no-restore
```

검사 범위는 권한·장착·배터리 조건, 70% 사거리 경계, 가장 가까운 대상 선택, 총구 오프셋과 고정 상하 각도를 포함한 360도 좌우 조준 계산, 중단 상태 전환, 실제 설치된 게임의 패치 대상·필드·사거리, Release 스폰 코드 제외입니다. 기본 Steam 경로 외에 설치했다면 검사 명령 끝에 `-- "<게임의 Assembly-CSharp.dll 경로>"`를 추가합니다. 이 검사는 게임 실행이나 실제 네트워크 플레이를 대신하지 않습니다.

## 프로젝트 구성

```text
src/PocketCartTurret/   플러그인 소스
planning/         개발 범위 및 수동 검증 항목
scripts/          게임 실행 및 패키징
thunderstore/     배포 메타데이터와 제출용 아이콘
.vscode/          빌드·디버깅·로그 작업
tests/            순수 규칙·조준 계산·게임 연결·Release 검사
```

## 탐지 경고음

적을 탐지하면 장착된 무기의 `transform.position`에 순정 Explosive Mine을 임시 생성하고, 무기의 위치·회전을 따라가게 합니다. 설치 과정의 삐삐삐 소리를 순정 간격으로 반복하며 추적·준비를 시작합니다. 추적이 연속 2초 유지되고 실제 총구가 정렬되어 사선이 확보되면 첫 발을 발사합니다. 발사 직전에 지뢰를 네트워크에서 제거합니다.

짧은 탐지 끊김도 연속 추적 시간을 초기화합니다. 재탐지부터 다시 2초를 채워야 발사합니다. 0.5초 미만 끊김에는 대기 중인 지뢰를 재사용하고, 0.5초 이상 놓치면 정리한 뒤 재탐지 때 새로 생성합니다. 같은 교전에서 가까운 적으로 타깃만 바뀌는 것은 추적 끊김으로 보지 않습니다. 연속 사격에는 순정 무기의 준비 시간을 사용합니다. 첫 발 이후 추적이 끊겼다가 재개되면 다시 경고하고 2초를 기다립니다.

장착 해제·배터리 소진·무기 직접 잡기·비활성화 시에는 즉시 정리합니다. 추적 대기 중에는 경고 지뢰를 유지하며, 제어 코드에서 관리가 끊긴 지뢰만 5초 뒤 정리합니다. 경고 지뢰의 충돌·잡기·토글·무장·폭발을 차단하며, 구매·보유 수량이나 저장 상태에 등록하지 않습니다. 일반 구매 지뢰는 정상 동작합니다.

일상 동작의 진단 로그와 F8 검사 코드는 제거했습니다. 플러그인 로드·오류 로그와 Debug 전용 스테이지 테스트 장비 스폰은 유지합니다. 코드 검토 결과와 남은 수동 검증 항목은 [코드 검토](planning/code-review.md)에 정리했습니다.
