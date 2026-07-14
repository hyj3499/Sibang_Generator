using System.IO;
using Sibang_generator.Models;

namespace Sibang_generator.Services;

/// <summary>
/// 펌웨어 루트 아래의 {BOM}_{Ver} 폴더를 읽는다.
/// 기존 SibangGenerator 에서 검증된 로직을 그대로 계승.
/// </summary>
public sealed class FirmwareScanner
{
    readonly string _root;
    readonly string[] _fixedPrefixes;

    public static readonly string[] DefaultFixedPrefixes =
        { "bootloader", "partition-table", "lgha_new_standard_rcu" };

    public bool Ready => Directory.Exists(_root);
    public string Root => _root;

    public FirmwareScanner(AppConfig cfg)
    {
        _root = cfg.FirmwareRoot ?? "";
        var parsed = (cfg.FixedFilePrefixes ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _fixedPrefixes = parsed.Length > 0 ? parsed : DefaultFixedPrefixes;
    }

    /// <summary>루트 아래 폴더 이름에서 버전을 수집한다. OHS3165F_1.00.7a → 1.00.7a</summary>
    public List<string> DiscoverVersions()
    {
        if (!Ready) return new();
        var versions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dir in Directory.GetDirectories(_root))
        {
            string name = Path.GetFileName(dir);
            int us = name.IndexOf('_');
            if (us > 0 && us < name.Length - 1) versions.Add(name[(us + 1)..]);
        }
        return versions.OrderBy(v => v, StringComparer.Ordinal).ToList();
    }

    public List<string> DiscoverBoms(string? version = null)
    {
        if (!Ready) return new();
        var boms = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dir in Directory.GetDirectories(_root))
        {
            string name = Path.GetFileName(dir);
            int us = name.IndexOf('_');
            if (us <= 0) continue;
            string bom = name[..us], ver = name[(us + 1)..];
            if (version is null || ver == version) boms.Add(bom);
        }
        return boms.OrderBy(b => b, StringComparer.Ordinal).ToList();
    }

    public bool FolderExists(string bom, string version) =>
        Ready && Directory.Exists(Path.Combine(_root, $"{bom}_{version}"));

    /// <summary>고정 3종 먼저(설정 순서), 나머지 파일명 오름차순. npprj 로 한정하지 않는다.</summary>
    public List<string> ReadFiles(string bom, string version)
    {
        string folder = Path.Combine(_root, $"{bom}_{version}");
        if (!Directory.Exists(folder)) return new();

        var all = Directory.GetFiles(folder)
            .Select(Path.GetFileName).OfType<string>()
            .OrderBy(f => f, StringComparer.Ordinal).ToList();

        var ordered = new List<string>();
        foreach (var prefix in _fixedPrefixes)
        {
            var hit = all.FirstOrDefault(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (hit is not null) { ordered.Add(hit); all.Remove(hit); }
        }
        ordered.AddRange(all);
        return ordered;
    }
}
