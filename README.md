# Sibang_generator (시방 제작툴)

엑셀(BOM) + 펌웨어 폴더로부터 시방을 처음부터 조립하는 WPF 도구입니다.

## 빌드 방법

### 필요 조건
- Windows 10/11 (x64)
- **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
  (SDK 없이 실행만 하려면 아래 "실행 파일 배포" 참고)

### 빌드
1. 압축을 풀고 폴더로 들어갑니다.
2. `build.cmd` 를 더블클릭하거나 명령창에서 실행합니다.
3. 완료되면 `publish\Sibang_generator.exe` 가 생성됩니다.

이 exe 는 **단일 파일 · 자체 포함**이라 .NET 런타임이 없는 PC 에서도 그대로 실행됩니다.
다른 PC 로 전달할 때는 `publish\Sibang_generator.exe` 하나만 복사하면 됩니다.

### 명령창에서 직접 빌드하려면
- dotnet publish Sibang_generator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish

### 개발/디버그 실행
- dotnet run

## 사용 흐름
1. 우측에서 **펌웨어 루트**와 **BOM 엑셀** 경로를 지정합니다.
2. 열 매핑(모델명/BOM/Region/Theme, 재작업 시트)이 실제 엑셀과 맞는지 확인합니다.
3. **시방 종류**(양산/재작업)를 고르고, 필요하면 옛 시방 txt 를 첨부해 상수 섹션(0,2,3,5,7,8,9)을 가져옵니다.
4. 좌측 **모델·버전 그룹**에 모델을 등록합니다. (버전당 한 그룹)
5. **6. 제조환경 단락 옵션**을 단락마다 지정합니다 (전부 / 등록만 / 관련그룹).
6. 가운데 하단 **[시방 제작]** → 미리보기로 전환. 한글/English 전환, 로그 확인, 저장.

## 설정 저장 위치
`%APPDATA%\Sibang_generator\settings.json`
경로·열 매핑·Region 라벨·상수 섹션·6번 골격 문구가 모두 저장되어 껐다 켜도 유지됩니다.
톱니바퀴(⚙ 설정)에서 Region 라벨과 한/영 템플릿을 편집할 수 있습니다.

## 프로젝트 구조
Models/        도메인 · 설정 · 템플릿
Services/      엑셀 조회, 폴더 스캔, 파서, 생성기, 설정 저장
ViewModels/    MVVM (MainViewModel)
Views/         화면 (MainWindow, 3개 대화상자)
Converters/    바인딩 컨버터
