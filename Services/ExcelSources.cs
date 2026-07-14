using System.IO;
using ClosedXML.Excel;
using Sibang_generator.Models;

namespace Sibang_generator.Services;

/// <summary>엑셀 열문자 ↔ 인덱스 유틸.</summary>
public static class Col
{
    /// <summary>"G" → 7, "AA" → 27. 1-based.</summary>
    public static int Index(string col)
    {
        int n = 0;
        foreach (char c in (col ?? "").ToUpperInvariant())
            if (c is >= 'A' and <= 'Z') n = n * 26 + (c - 'A' + 1);
        return Math.Max(n, 1);
    }
}

/// <summary>
/// BOM 엑셀 조회. 기존 SibangGenerator 의 BomLookup 을 확장해
/// BOM 뿐 아니라 Region · Theme 도 같은 행에서 읽는다.
///
/// 핵심 규칙(계승): 한 셀에 여러 모델이 줄바꿈으로 들어있을 수 있으므로
/// 분해 후 정확일치로 찾는다. 그래야 ENCXUAEC 와 ENCXUAECRC 가 안 섞인다.
/// </summary>
public sealed class BomLookup : IDisposable
{
    readonly XLWorkbook? _wb;
    readonly IXLWorksheet? _sheet;
    readonly int _bomCol, _regionCol, _themeCol, _lastRow;
    readonly int[] _matchCols;
    readonly bool _splitCell;

    public bool Ready => _sheet is not null;
    public string? Error { get; }

    public BomLookup(AppConfig cfg)
    {
        _splitCell = cfg.SplitCell;
        _matchCols = (cfg.MatchColumn ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Col.Index).ToArray();
        _bomCol = Col.Index(cfg.BomColumn);
        _regionCol = Col.Index(cfg.RegionColumn);
        _themeCol = Col.Index(cfg.ThemeColumn);

        if (string.IsNullOrWhiteSpace(cfg.ExcelPath))
        { Error = "엑셀 경로가 지정되지 않았습니다."; return; }
        if (!File.Exists(cfg.ExcelPath))
        { Error = $"엑셀 파일을 찾을 수 없습니다: {cfg.ExcelPath}"; return; }
        if (_matchCols.Length == 0)
        { Error = "엑셀 모델명 매칭 열이 지정되지 않았습니다."; return; }

        try
        {
            _wb = new XLWorkbook(cfg.ExcelPath);
            _sheet = _wb.Worksheets.FirstOrDefault(x => x.Name == cfg.SheetName)
                     ?? _wb.Worksheet(1);
            _lastRow = _sheet.LastRowUsed()?.RowNumber() ?? 0;
        }
        catch (Exception ex)
        {
            Error = $"엑셀을 열 수 없습니다: {ex.Message}";
        }
    }

    public readonly record struct Row(BomFindResult Bom, string? Region, string? Theme);

    /// <summary>모델명으로 BOM · Region · Theme 를 한 번에 찾는다.</summary>
    public Row Find(string model)
    {
        if (_sheet is null) return new(BomFindResult.NotFound, null, null);

        for (int r = 1; r <= _lastRow; r++)
        {
            foreach (int matchCol in _matchCols)
            {
                var cell = _sheet.Cell(r, matchCol);
                string cellValue = cell.GetString();
                if (cellValue.Length == 0) continue;

                var candidates = _splitCell
                    ? cellValue.Split(new[] { '\r', '\n', ',', ';' },
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : new[] { cellValue.Trim() };

                if (!candidates.Any(c => string.Equals(c, model, StringComparison.Ordinal)))
                    continue;

                string? region = Read(r, _regionCol);
                string? theme = Read(r, _themeCol);

                if (cell.IsMerged())
                    return new(BomFindResult.MergedCell, region, theme);

                string bom = _sheet.Cell(r, _bomCol).GetString().Trim();
                var res = bom.Length > 0 ? BomFindResult.Found(bom) : BomFindResult.NotFound;
                return new(res, region, theme);
            }
        }
        return new(BomFindResult.NotFound, null, null);
    }

    /// <summary>
    /// 엑셀에 있는 모든 모델을 (모델, Region, Theme) 로 수집한다.
    /// 6번 "전부 파싱" 옵션에서 사용한다.
    /// </summary>
    public List<(string Model, string? Region, string? Theme)> All()
    {
        var list = new List<(string, string?, string?)>();
        if (_sheet is null) return list;

        for (int r = 1; r <= _lastRow; r++)
        {
            string? region = Read(r, _regionCol);
            string? theme = Read(r, _themeCol);

            foreach (int matchCol in _matchCols)
            {
                var cell = _sheet.Cell(r, matchCol);
                string cellValue = cell.GetString();
                if (cellValue.Length == 0) continue;

                var candidates = _splitCell
                    ? cellValue.Split(new[] { '\r', '\n', ',', ';' },
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : new[] { cellValue.Trim() };

                foreach (var c in candidates)
                    list.Add((c, region, theme));
            }
        }
        return list;
    }

    /// <summary>
    /// 셀 값을 읽되, 병합된 셀이면 병합영역 좌상단(값이 실제로 들어있는 셀)을 읽는다.
    /// 예: X8,X9,X10 이 병합돼 있으면 X8 에만 값이 있으므로
    /// X9, X10 을 조회해도 X8 의 Region 값을 돌려준다.
    /// </summary>
    string? Read(int r, int c)
    {
        if (_sheet is null) return null;

        var cell = _sheet.Cell(r, c);
        var v = cell.GetString().Trim();

        // 값이 비어있고 병합 셀이면 병합영역 좌상단 값을 읽는다
        if (v.Length == 0 && cell.IsMerged())
        {
            var merged = cell.MergedRange();
            if (merged is not null)
                v = merged.FirstCell().GetString().Trim();
        }
        return v.Length == 0 ? null : v;
    }

    public void Dispose() => _wb?.Dispose();
}

/// <summary>
/// 재작업 관련 시트 조회.
/// L열 = 모델명, M열 = HW Function(Wi-Fi/Buzzer/-), N열 = 구미 재작업 테스트 항목.
/// 열/시트는 사용자가 지정 (기본 L/M/N).
/// </summary>
public sealed class ReworkLookup : IDisposable
{
    readonly XLWorkbook? _wb;
    readonly IXLWorksheet? _sheet;
    readonly int _modelCol, _hwCol, _testCol, _lastRow;

    public bool Ready => _sheet is not null;
    public string? Error { get; }

    public ReworkLookup(AppConfig cfg)
    {
        _modelCol = Col.Index(cfg.ReworkModelColumn);
        _hwCol = Col.Index(cfg.HwFunctionColumn);
        _testCol = Col.Index(cfg.ReworkTestColumn);

        if (string.IsNullOrWhiteSpace(cfg.ExcelPath) || !File.Exists(cfg.ExcelPath))
        { Error = "엑셀 파일이 없어 재작업 시트를 열 수 없습니다."; return; }

        try
        {
            _wb = new XLWorkbook(cfg.ExcelPath);
            _sheet = _wb.Worksheets.FirstOrDefault(x => x.Name == cfg.ReworkSheet);
            if (_sheet is null)
            { Error = $"재작업 시트 '{cfg.ReworkSheet}' 를 찾을 수 없습니다."; return; }
            _lastRow = _sheet.LastRowUsed()?.RowNumber() ?? 0;
        }
        catch (Exception ex)
        {
            Error = $"재작업 시트를 열 수 없습니다: {ex.Message}";
        }
    }

    /// <summary>모델명 → (HW Function, 테스트 항목). 정확일치.</summary>
    public (string? Hw, string? Tests) Find(string model)
    {
        if (_sheet is null) return (null, null);

        for (int r = 1; r <= _lastRow; r++)
        {
            var cellValue = _sheet.Cell(r, _modelCol).GetString();
            if (cellValue.Length == 0) continue;

            var candidates = cellValue.Split(new[] { '\r', '\n', ',', ';' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!candidates.Any(c => string.Equals(c, model, StringComparison.Ordinal)))
                continue;

            string hw = _sheet.Cell(r, _hwCol).GetString().Trim();
            string tests = _sheet.Cell(r, _testCol).GetString().Trim();
            return (hw.Length > 0 ? hw : null, tests.Length > 0 ? tests : null);
        }
        return (null, null);
    }

    public void Dispose() => _wb?.Dispose();
}
