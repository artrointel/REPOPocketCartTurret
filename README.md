# MiniTurret

R.E.P.O.용 MiniTurret 모드 개발 프로젝트입니다. REPORaiseToUpgrade와 같은 개발 환경을 사용하며, 현재는 플러그인 로딩과 Harmony 초기화까지 준비되어 있습니다. 터렛 동작은 아직 구현하지 않았습니다.

## 개발 환경

- .NET 9 SDK, C# 12, `netstandard2.1`
- BepInEx 5.4.21, HarmonyX 2.7.0
- UnityEngine.Modules 2022.3.62
- R.E.P.O.GameLibs.Steam 0.4.4-ngd.0
- Linkoid.Repo.Plugin.Build 0.2.2

기준 프로젝트에서 실제 복원된 BepInEx 버전을 고정했습니다. 로컬 `.dotnet/` SDK와 `.nuget/packages/` 캐시는 Git에서 제외됩니다. 새 PC에서는 `global.json`에 맞는 .NET 9 SDK를 준비해야 합니다. VS Code 작업은 `.dotnet/dotnet.exe`를 사용합니다.

## 빌드 및 실행

현재 로컬 `Directory.Repo.props`는 Steam 기본 설치 경로와 Thunderstore Mod Manager의 `REPO` 프로필을 사용합니다. 빌드 시 DLL/PDB가 `BepInEx/plugins/MiniTurret/`에 배포됩니다.

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:NUGET_PACKAGES = "$PWD\.nuget\packages"
.\.dotnet\dotnet.exe restore src\MiniTurret\MiniTurret.csproj
.\.dotnet\dotnet.exe build src\MiniTurret\MiniTurret.csproj --configuration Debug --no-restore
```

이미 준비된 로컬 캐시만으로 복원하려면 다음 명령을 사용합니다.

```powershell
.\.dotnet\dotnet.exe restore src\MiniTurret\MiniTurret.csproj --source "$PWD\.nuget\packages" -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false
```

게임 프로필에 배포하지 않고 빌드만 확인하려면 `-p:CopyFilesToPluginOutputDirectoryOnBuild=false`를 빌드 명령에 추가합니다.

- `Ctrl+Shift+B`: Debug 빌드 및 개발 프로필에 배포
- `Tasks: Run Task` → `MiniTurret: Start game`: 빌드·배포 후 게임 실행
- `F5` → `MiniTurret: Launch and attach`: Mono 디버거로 실행 및 연결 (`127.0.0.1:10000`)
- `MiniTurret: Watch BepInEx log`: 개발 프로필 로그 확인

F5에는 VS Code의 C# Dev Kit과 Unity 확장이 필요합니다. 게임을 종료한 뒤 코드를 다시 빌드하고 실행하세요. 로딩 성공 시 `MiniTurret v0.1.0 loaded.`가 기록됩니다.

다른 프로필은 `Directory.Repo.props.example` 또는 `Directory.Repo.Gale.props.example`을 `Directory.Repo.props`로 복사해 지정할 수 있습니다. 실행 스크립트와 로그 작업의 경로도 함께 변경하세요. `Directory.Repo.props`는 개인 경로를 포함하므로 Git에서 제외됩니다.

## Thunderstore 패키지

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Pack-Thunderstore.ps1
```

결과는 `artifacts/MiniTurret-0.1.0.zip`에 생성됩니다. 패키징 빌드는 게임 프로필에 배포하지 않습니다. 현재 `thunderstore/icon.png`는 개발용 임시 아이콘이며 공개 배포 전에 교체해야 합니다. 버전은 csproj, `Plugin.cs`, `thunderstore/manifest.json` 세 곳에서 일치해야 합니다.

복원 완료 후 네트워크 없이 패키징하려면 `-NoRestore`를 추가합니다.

## 프로젝트 구성

```text
src/MiniTurret/   플러그인 소스
planning/         개발 범위 및 수동 검증 항목
scripts/          게임 실행 및 패키징
thunderstore/     배포 메타데이터와 임시 아이콘
.vscode/          빌드·디버깅·로그 작업
```
