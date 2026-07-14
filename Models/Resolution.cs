using System.ComponentModel;

namespace Sibang_generator.Models;

// ═══════════════════════════════════════════════════════════
//  모델 해석 (기존 SibangGenerator 에서 검증된 로직을 그대로 계승)
//
//  버전 그룹 → 모델 하나하나로 펼치고, 엑셀에서 BOM 을,
//  디스크에서 폴더 · 파일 목록을 채운다.
// ═══════════════════════════════════════════════════════════

public enum Severity { Error, Warning, Info, Pass }

public sealed class Issue
{
    public Severity Sev { get; init; }
    public int Line { get; init; }
    public string Msg { get; init; } = "";
}

/// <summary>
/// 사용자가 등록하는 단위. 버전 하나에 모델 여러 개.
/// 같은 버전을 두 번 등록할 수 없으므로 한 모델은 한 버전만 갖는다.
/// </summary>
public sealed class VersionGroup
{
    public string Version { get; set; } = "";
    /// <summary>사용자가 복붙한 원본 텍스트. 편집 시 그대로 보여준다.</summary>
    public string RawModels { get; set; } = "";
    /// <summary>파싱된 모델명. 등록 순서를 유지한다.</summary>
    public List<string> Models { get; set; } = new();
    /// <summary>모델명 → 사용자가 직접 지정한 BOM.</summary>
    public Dictionary<string, string> BomOverrides { get; set; } = new();

    public string Summary => $"{Models.Count}개 모델";
}

/// <summary>BOM 조회 결과. 단순 실패와 "병합 셀이라 판단 불가"를 구분한다.</summary>
public readonly struct BomFindResult
{
    public string? Bom { get; init; }
    public bool Merged { get; init; }

    public static BomFindResult Found(string bom) => new() { Bom = bom };
    public static BomFindResult NotFound => new();
    public static BomFindResult MergedCell => new() { Merged = true };

    public bool HasBom => !string.IsNullOrEmpty(Bom);
}

/// <summary>
/// 엑셀 · 폴더에서 채운 모델 하나의 완성된 정보.
/// Region/Theme/HW Function 은 이번 도구에서 새로 추가된 축이다.
/// </summary>
public sealed class ResolvedModel : INotifyPropertyChanged
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public int Order { get; init; }

    // ── BOM (4. 제품버전용) ──────────────────────
    public string? ExcelBom { get; set; }
    public bool MergedCell { get; set; }

    string? _manualBom;
    public string? ManualBom
    {
        get => _manualBom;
        set { _manualBom = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); NotifyAll(); }
    }

    public string? Bom => ManualBom ?? ExcelBom;
    public bool IsOverridden => ManualBom is not null;
    public string? FolderName => Bom is null ? null : $"{Bom}_{Version}";
    public bool BomFound => !string.IsNullOrEmpty(Bom);

    bool _folderFound;
    public bool FolderFound
    {
        get => _folderFound;
        set { _folderFound = value; NotifyAll(); }
    }

    public List<string> Files { get; set; } = new();

    // ── 6. 제조환경용 축 (신규) ──────────────────
    /// <summary>BOM 엑셀에서 읽은 Region (예: UAE, KOR, GLOBAL).</summary>
    public string? Region { get; set; }
    /// <summary>BOM 엑셀에서 읽은 Theme (예: white). 항상 white 아님.</summary>
    public string? Theme { get; set; }
    /// <summary>재작업 시트에서 읽은 HW Function (Wi-Fi / Buzzer / -).</summary>
    public string? HwFunction { get; set; }
    /// <summary>재작업 시트에서 읽은 구미 재작업 테스트 항목 원본(줄바꿈 포함).</summary>
    public string? ReworkTests { get; set; }

    /// <summary>Region 에서 유도한 Language. KOR 이면 KOR, 아니면 ENG.</summary>
    public string Language =>
        string.Equals(Region, "KOR", StringComparison.OrdinalIgnoreCase) ? "KOR" : "ENG";

    public string Status =>
        MergedCell ? "셀 병합됨 — 클릭해서 직접 지정"
        : !BomFound ? "BOM 조회 실패 — 클릭해서 직접 지정"
        : !FolderFound ? $"{Bom} · 폴더 없음"
        : $"{Bom} · 파일 {Files.Count}개"
          + (Region is null ? " · Region?" : $" · {Region}")
          + (HwFunction is null ? "" : $"/{HwFunction}");

    public string OverrideTag => IsOverridden ? "직접 지정" : "";

    public Severity StatusSev =>
        !BomFound || !FolderFound ? Severity.Error
        : Region is null ? Severity.Warning
        : Severity.Pass;

    public event PropertyChangedEventHandler? PropertyChanged;

    void NotifyAll()
    {
        foreach (var n in new[] { nameof(Bom), nameof(BomFound), nameof(FolderName),
                                  nameof(Status), nameof(StatusSev), nameof(IsOverridden),
                                  nameof(OverrideTag), nameof(FolderFound) })
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

/// <summary>(BOM, Ver) 쌍으로 묶인 4-N 블록 하나.</summary>
public sealed class ProductVersionBlock
{
    public string Bom { get; init; } = "";
    public string Version { get; init; } = "";
    public List<string> Models { get; init; } = new();
    public List<string> Files { get; init; } = new();
    public string Cpu { get; set; } = "";
    public string FolderName => $"{Bom}_{Version}";
}

/// <summary>생성 결과와 그 과정에서 나온 로그.</summary>
public sealed class GenerationResult
{
    public string[] KoLines { get; init; } = Array.Empty<string>();
    public string[] EnLines { get; init; } = Array.Empty<string>();
    public List<Issue> Log { get; init; } = new();

    public int ErrorCount => Log.Count(x => x.Sev == Severity.Error);
    public int WarnCount => Log.Count(x => x.Sev == Severity.Warning);
}
