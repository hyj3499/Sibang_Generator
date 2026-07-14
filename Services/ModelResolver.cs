using Sibang_generator.Models;

namespace Sibang_generator.Services;

/// <summary>
/// 버전 그룹을 실제 데이터로 채우고, 4-N 블록 단위로 다시 묶는다.
/// 기존 도구의 로직을 계승하되, Region/Theme/HW/테스트 축을 추가로 채운다.
/// </summary>
public static class ModelResolver
{
    public static List<ResolvedModel> Resolve(
        IEnumerable<VersionGroup> groups,
        BomLookup? bom,
        ReworkLookup? rework,
        FirmwareScanner? fw)
    {
        var result = new List<ResolvedModel>();
        int order = 0;

        foreach (var g in groups)
            foreach (var name in g.Models)
            {
                var m = new ResolvedModel { Name = name, Version = g.Version, Order = order++ };

                // 1) BOM 엑셀 조회 (BOM + Region + Theme)
                if (bom is not null && bom.Ready)
                {
                    var row = bom.Find(name);
                    if (row.Bom.Merged) m.MergedCell = true;
                    else m.ExcelBom = row.Bom.Bom;
                    m.Region = row.Region;
                    m.Theme = row.Theme;
                }

                // 2) 사용자 지정 BOM 우선
                if (g.BomOverrides.TryGetValue(name, out var manual) &&
                    !string.IsNullOrWhiteSpace(manual))
                    m.ManualBom = manual;

                // 3) 재작업 시트 조회 (HW Function + 테스트 항목)
                if (rework is not null && rework.Ready)
                {
                    var (hw, tests) = rework.Find(name);
                    m.HwFunction = hw;
                    m.ReworkTests = tests;
                }

                // 4) 폴더 확인
                if (m.Bom is not null && fw is not null && fw.Ready)
                {
                    bool exists = fw.FolderExists(m.Bom, m.Version);
                    if (exists) m.Files = fw.ReadFiles(m.Bom, m.Version);
                    m.FolderFound = exists;
                }
                result.Add(m);
            }

        return result;
    }

    /// <summary>(BOM, Ver) 쌍으로 묶어 4-N 블록을 만든다. 입력 순서 보존.</summary>
    public static List<ProductVersionBlock> BuildBlocks(
        IEnumerable<ResolvedModel> models, AppConfig cfg)
    {
        return models
            .Where(m => m.BomFound && m.FolderFound)
            .GroupBy(m => (m.Bom!, m.Version))
            .OrderBy(g => g.Min(m => m.Order))
            .Select(g => new ProductVersionBlock
            {
                Bom = g.Key.Item1,
                Version = g.Key.Item2,
                Models = g.OrderBy(m => m.Order).Select(m => m.Name).ToList(),
                Files = g.First().Files,
                Cpu = cfg.Cpu
            })
            .ToList();
    }

    /// <summary>텍스트박스에 복붙한 모델명을 파싱한다. 순서 유지, 중복 제거.</summary>
    public static List<string> ParseModels(string raw)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var t in raw.Split(new[] { ',', '\n', '\r', '\t', ';' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (seen.Add(t)) list.Add(t);
        return list;
    }
}
