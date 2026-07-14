using System.Text.Json.Serialization;

namespace Sibang_generator.Models;

/// <summary>시방 종류 = 레이아웃 결정.</summary>
public enum SpecKind
{
    /// <summary>양산 시방 (구미 재작업 공정 없음).</summary>
    MassProduction,
    /// <summary>재작업 (6-1 구미 재작업 공정 포함, 기존 구조가 6-2 로 밀림).</summary>
    Rework
}

/// <summary>
/// 6. 제조환경의 모델 나열 단락에 적용되는 옵션.
/// 각 단락(문단)마다 개별 설정 가능.
/// </summary>
public enum ParaMode
{
    /// <summary>전부 파싱해서 가져오는 옵션 (엑셀에 있는 모든 모델).</summary>
    All,
    /// <summary>입력된(등록된) 모델만 적는 옵션.</summary>
    RegisteredOnly,
    /// <summary>입력된 모델이 있는 그룹(같은 Region/Theme 또는 HW)만 가져오는 옵션.</summary>
    RelatedGroups
}

/// <summary>
/// 6번 안에서 모델이 나열되는 개별 단락.
/// 단락마다 ParaMode 를 다르게 줄 수 있다.
/// </summary>
public enum Para
{
    /// <summary>1) CMD 전송 → TESTMODE REGION/THEME 나열.</summary>
    CmdRegionTheme,
    /// <summary>LCD 1) Wi-Fi / Buzzer / - 표시 분류.</summary>
    HwFunction,
    /// <summary>LCD 9) Region, Language, Theme 향(向) 나열.</summary>
    RegionLangTheme,
    /// <summary>6-1) 구미 재작업 공정 테스트 항목 (재작업 시방에만).</summary>
    ReworkGumi
}

/// <summary>단락별 옵션 설정 (Para → ParaMode).</summary>
public sealed class ParaOption
{
    public Para Para { get; set; }
    public ParaMode Mode { get; set; } = ParaMode.RelatedGroups;

    [JsonIgnore] public string ParaLabel => Para switch
    {
        Para.CmdRegionTheme   => "① CMD 전송 (REGION/THEME)",
        Para.HwFunction       => "② Wi-Fi / Buzzer 표시",
        Para.RegionLangTheme  => "③ Region · Language · Theme (向)",
        Para.ReworkGumi       => "⓪ 구미 재작업 테스트 (6-1)",
        _ => Para.ToString()
    };

    [JsonIgnore] public string ModeLabel => Mode switch
    {
        ParaMode.All            => "전부 파싱",
        ParaMode.RegisteredOnly => "등록 모델만",
        ParaMode.RelatedGroups  => "관련 그룹만",
        _ => Mode.ToString()
    };
}

/// <summary>
/// 6-1 구미 재작업 테스트 항목의 한↔영 대응 한 줄.
/// Ko(한글 문구) ↔ En(영문 문구). 번호는 제외한 순수 문구로 저장한다.
/// </summary>
public sealed class ReworkTestPair
{
    public string Ko { get; set; } = "";
    public string En { get; set; } = "";

    /// <summary>사용자 제공 데이터에서 뽑은 기본 대응표 (줄 단위).</summary>
    public static List<ReworkTestPair> Defaults() => new()
    {
        new() { Ko = "F/W Copy", En = "F/W Copy" },
        new() { Ko = "[TestMode] 지역 UAE 변경후 UserMode전환 → UserMode로 부팅",
                En = "[TestMode] Change region to UAE and switch to UserMode → Boot in UserMode" },
        new() { Ko = "[TestMode] 지역 ASIA 변경후 UserMode전환 → UserMode로 부팅",
                En = "[TestMode] Change region to ASIA and switch to UserMode → Boot in UserMode" },
        new() { Ko = "[TestMode] 지역 KOR 변경후 UserMode전환 → UserMode로 부팅",
                En = "[TestMode] Change region to KOR and switch to UserMode → Boot in UserMode" },
        new() { Ko = "[TestMode] 지역 GLOBAL 변경후 UserMode전환 → UserMode로 부팅",
                En = "[TestMode] Change region to GLOBAL and switch to UserMode → Boot in UserMode" },
        new() { Ko = "[TestMode] 지역 EGY 변경후 UserMode전환 → UserMode로 부팅",
                En = "[TestMode] Change region to EGY and switch to UserMode → Boot in UserMode" },
        new() { Ko = "[UserMode] OTP 모델 등록 (PREMTB200)",
                En = "[UserMode] Register OTP Model (PREMTB200)" },
        new() { Ko = "[UserMode] Wi-Fi 테스트",
                En = "[UserMode] Wi-Fi Test" },
        new() { Ko = "[UserMode] SW 버전 및 모델명 (PREMTB200) 확인",
                En = "[UserMode] Verify S/W version and model name (PREMTB200)" },
        new() { Ko = "[UserMode] SW 버전 및 모델명 (PREMTB201) 확인",
                En = "[UserMode] Verify S/W version and model name (PREMTB201)" },
        new() { Ko = "[UserMode] 냉방 최소온도 설정 범위 20도 확인",
                En = "[UserMode] Verify minimum cooling temperature range: 20 degrees" },
        new() { Ko = "[UserMode] 난방 최대온도 설정 범위 30도 확인",
                En = "[UserMode] Verify maximum heating temperature range: 30 degrees" },
        new() { Ko = "[UserMode] 난방 최대온도 설정 범위 28도 확인",
                En = "[UserMode] Verify maximum heating temperature range: 28 degrees" },
        new() { Ko = "[UserMode] 언어설정에서 'English' 만 있는지 확인",
                En = "[UserMode] Verify language settings: English only" },
        new() { Ko = "[UserMode] 냉방 최소온도 설정 범위 16 or 18도 확인",
                En = "[UserMode] Verify minimum cooling temperature range: 16 or 18 degrees" },
        new() { Ko = "[UserMode] 언어설정에서 '한국어, English' 2개 있는지 확인",
                En = "[UserMode] Verify language settings: Korean, English (2 languages)" },
        new() { Ko = "[UserMode] 언어설정에서 14개국 언어 있는지 확인",
                En = "[UserMode] Verify language settings: 14 languages" },
    };
}

/// <summary>
/// Region → 한글/영문 라벨 매핑 한 줄.
/// 예: UAE → "UAE향" / "For UAE", Language ENG.
/// </summary>
public sealed class RegionLabel
{
    public string Region { get; set; } = "";
    public string Ko { get; set; } = "";
    public string En { get; set; } = "";

    /// <summary>비면 Region 에서 유도 (KOR→KOR, else ENG).</summary>
    public string Language { get; set; } = "";

    public string EffectiveLanguage =>
        !string.IsNullOrWhiteSpace(Language) ? Language
        : string.Equals(Region, "KOR", StringComparison.OrdinalIgnoreCase) ? "KOR" : "ENG";
}

/// <summary>
/// 프로그램을 껐다 켜도 유지되는 전체 설정.
/// 저장 위치: %APPDATA%\Sibang_generator\settings.json
/// </summary>
public sealed class AppConfig
{
    // ── 경로 · 엑셀 (BOM) ────────────────────────
    public string FirmwareRoot { get; set; } = "";
    public string ExcelPath { get; set; } = "";
    public string SheetName { get; set; } = "Sheet1";
    public string MatchColumn { get; set; } = "G";     // 모델명 매칭 열 (복수 가능, 콤마)
    public string BomColumn { get; set; } = "K";       // BOM 값 열
    public string RegionColumn { get; set; } = "X";    // Region 열
    public string ThemeColumn { get; set; } = "AA";    // Theme 열
    public bool SplitCell { get; set; } = true;

    // ── 재작업 관련 시트 (HW Function / 테스트 항목) ──
    public string ReworkSheet { get; set; } = "재작업";
    public string ReworkModelColumn { get; set; } = "L";   // 모델명
    public string HwFunctionColumn { get; set; } = "M";    // Wi-Fi/Buzzer/-
    public string ReworkTestColumn { get; set; } = "N";    // 테스트 항목 통째

    // ── 4. 제품버전 ──────────────────────────────
    public string Cpu { get; set; } = "ESP32-S3R16 (ESPRESSIF)";
    public string FixedFilePrefixes { get; set; } =
        "bootloader, partition-table, lgha_new_standard_rcu";

    // ── Region → 라벨 매핑 (편집 가능, 기본값 하드코딩) ──
    public List<RegionLabel> RegionLabels { get; set; } = DefaultRegionLabels();

    // ── 상수 섹션 (0,2,3,5,7,8,9) 한/영 템플릿 ──────
    public ConstSections Ko { get; set; } = ConstSections.DefaultKo();
    public ConstSections En { get; set; } = ConstSections.DefaultEn();

    // ── 6번 고정 골격 한/영 문구 사전 ──────────────
    public Section6Template Ko6 { get; set; } = Section6Template.DefaultKo();
    public Section6Template En6 { get; set; } = Section6Template.DefaultEn();

    // ── 6-1 구미 재작업 테스트 한↔영 번역 사전 ──────
    //  재작업 시트(N열)의 테스트 항목은 한글이다.
    //  영문 시방을 만들 때 이 사전으로 줄 단위 번역한다.
    //  키(한글)는 번호를 뗀 순수 문구로 저장/조회한다.
    public List<ReworkTestPair> ReworkTestDict { get; set; } = ReworkTestPair.Defaults();

    public static List<RegionLabel> DefaultRegionLabels() => new()
    {
        new() { Region = "UAE",    Ko = "UAE향",            En = "For UAE",                    Language = "ENG" },
        new() { Region = "ASIA",   Ko = "사우디, 중아향",    En = "For Saudi Arabia, Central Asia", Language = "ENG" },
        new() { Region = "KOR",    Ko = "내수향",            En = "For KOR",                    Language = "KOR" },
        new() { Region = "GLOBAL", Ko = "유럽향",            En = "For Europe",                 Language = "ENG" },
        new() { Region = "US",     Ko = "북미향",            En = "For US",                     Language = "ENG" },
        new() { Region = "EGY",    Ko = "이집트향",          En = "For EGY",                    Language = "ENG" },
    };
}
