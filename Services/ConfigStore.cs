using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sibang_generator.Models;

namespace Sibang_generator.Services;

/// <summary>
/// 프로그램을 껐다 켜도 유지되는 설정 저장소.
/// 저장 위치: %APPDATA%\Sibang_generator\settings.json
///
/// 경로 · 열 매핑 · Region 라벨 · 영문 사전 · 6번 템플릿을 모두 기억한다.
/// 저장/불러오기 실패는 조용히 무시한다 — 설정이 작업을 막으면 안 된다.
/// </summary>
public static class ConfigStore
{
    static readonly JsonSerializerOptions Opt = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // 한글이 \uXXXX 로 깨지지 않게
        Converters = { new JsonStringEnumConverter() }
    };

    public static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Sibang_generator");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppConfig();
            var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath), Opt)
                      ?? new AppConfig();
            // 빈 목록으로 저장된 경우 기본값 복구
            if (cfg.RegionLabels.Count == 0) cfg.RegionLabels = AppConfig.DefaultRegionLabels();
            return cfg;
        }
        catch { return new AppConfig(); }
    }

    public static void Save(AppConfig cfg)
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(cfg, Opt), new UTF8Encoding(true));
        }
        catch { /* 무시 */ }
    }
}

/// <summary>생성된 시방을 저장한다.</summary>
public static class SpecWriter
{
    public static void Write(string path, IEnumerable<string> lines) =>
        File.WriteAllText(path, string.Join("\r\n", lines), new UTF8Encoding(true));

    public static string BuildLog(GenerationResult r, IReadOnlyList<VersionGroup> groups)
    {
        var sb = new StringBuilder();
        sb.AppendLine("시방 생성 로그");
        sb.AppendLine($"생성  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("등록 모델");
        foreach (var g in groups)
            sb.AppendLine($"  [{g.Version}]  {string.Join(", ", g.Models)}");
        sb.AppendLine();
        sb.AppendLine($"요약  오류 {r.ErrorCount} · 경고 {r.WarnCount} · 한글 {r.KoLines.Length}줄 · 영문 {r.EnLines.Length}줄");
        sb.AppendLine();

        foreach (var sev in new[] { Severity.Error, Severity.Warning, Severity.Info })
        {
            var rows = r.Log.Where(x => x.Sev == sev).ToList();
            if (rows.Count == 0) continue;
            string label = sev switch { Severity.Error => "오류", Severity.Warning => "경고", _ => "정보" };
            sb.AppendLine($"[{label}] {rows.Count}건");
            sb.AppendLine(new string('─', 60));
            foreach (var x in rows) sb.AppendLine($"  {x.Msg}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
