using System.Text.RegularExpressions;
using Sibang_generator.Models;

namespace Sibang_generator.Services;

/// <summary>
/// 시방을 처음부터 조립한다.
///
/// 0~9 골격은 고정. 1, 4, 6 만 모델에 따라 생성하고
/// 0,2,3,5,7,8,9 는 상수(또는 기존 시방 첨부)에서 그대로 가져온다.
///
/// 한글/영문을 한 번에 만든다. 고정 문구는 한/영 템플릿 두 벌을 쓰고
/// 모델 리스트만 양쪽에 동일하게 넣는다.
/// </summary>
public sealed class SpecGenerator
{
    readonly AppConfig _cfg;
    readonly List<Issue> _log = new();
    readonly HashSet<string> _known;

    public SpecGenerator(AppConfig cfg)
    {
        _cfg = cfg;
        _known = new HashSet<string>(
            cfg.KnownModels ?? new List<string>(), StringComparer.Ordinal);
    }

    /// <summary>
    /// 화이트리스트가 있으면, 그 안에 든 모델만 통과시킨다.
    /// 화이트리스트가 비어 있으면(사용자가 다 지운 경우) 필터를 적용하지 않는다.
    /// </summary>
    bool IsKnown(string name) => _known.Count == 0 || _known.Contains(name);

    void Err(string m) => _log.Add(new Issue { Sev = Severity.Error, Msg = m });
    void Warn(string m) => _log.Add(new Issue { Sev = Severity.Warning, Msg = m });
    void Info(string m) => _log.Add(new Issue { Sev = Severity.Info, Msg = m });

    /// <summary>단락별 옵션을 Para → ParaMode 로 조회하기 쉽게.</summary>
    static Dictionary<Para, ParaMode> ToMap(IEnumerable<ParaOption> opts) =>
        opts.ToDictionary(o => o.Para, o => o.Mode);

    public GenerationResult Generate(
        SpecKind kind,
        IReadOnlyList<VersionGroup> groups,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcelModels,
        Func<string, (string? Hw, string? Tests)> reworkFor,
        IReadOnlyList<ParaOption> paraOptions,
        ConstSections ko,
        ConstSections en)
    {
        _log.Clear();
        var paras = ToMap(paraOptions);

        // 등록 모델 중 실제 사용 가능한 것
        var usable = registered.Where(m => m.BomFound).ToList();

        var koLines = BuildOneLanguage(true, kind, groups, registered, usable, allExcelModels, reworkFor, paras, ko);
        var enLines = BuildOneLanguage(false, kind, groups, registered, usable, allExcelModels, reworkFor, paras, en);

        var lines = new List<string>();
        lines.AddRange(koLines);
        lines.Add("");
        lines.Add("");
        lines.AddRange(enLines);

        return new GenerationResult
        {
            KoLines = koLines.ToArray(),
            EnLines = enLines.ToArray(),
            Log = _log.ToList()
        };
    }

    // ══════════════════════════════════════════════════════
    //  한 언어(한글 또는 영문) 전체 조립
    // ══════════════════════════════════════════════════════
    List<string> BuildOneLanguage(
        bool koLang,
        SpecKind kind,
        IReadOnlyList<VersionGroup> groups,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<ResolvedModel> usable,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcelModels,
        Func<string, (string? Hw, string? Tests)> reworkFor,
        Dictionary<Para, ParaMode> paras,
        ConstSections cs)
    {
        var t6 = koLang ? _cfg.Ko6 : _cfg.En6;
        var L = new List<string>();

        // 0. 시방목적
        AddBlock(L, cs.S0);
        // 1. 모델이름
        L.Add(Sec1(koLang, registered));
        // 2. 모델타입
        AddBlock(L, cs.S2);
        // 3. 적용시점
        AddBlock(L, cs.S3);
        // 4. 제품버전
        L.AddRange(Sec4(koLang, usable));
        // 5. 변경내용
        AddBlock(L, cs.S5);
        // 6. 제조환경
        L.AddRange(Sec6(koLang, kind, groups, registered, usable, allExcelModels, reworkFor, paras, t6));
        // 7,8,9
        AddBlock(L, cs.S7);
        AddBlock(L, cs.S8);
        AddBlock(L, cs.S9);

        // 섹션 끝 마커: 한글은 "- 이 상 -", 영문은 "- END -"
        L.Add("");
        L.Add(koLang ? "- 이 상 -" : "- END -");
        return L;
    }

    static void AddBlock(List<string> L, string block)
    {
        if (string.IsNullOrEmpty(block)) return;
        foreach (var line in block.Replace("\r\n", "\n").Split('\n'))
            L.Add(line);
    }

    // ── 1. 모델이름 ──────────────────────────────────
    static string Sec1(bool ko, IReadOnlyList<ResolvedModel> registered)
    {
        string names = string.Join(", ", registered.Select(m => m.Name));
        return ko ? $"1. 모델이름 : {names}" : $"1. Model Name : {names}";
    }

    // ── 4. 제품버전 ──────────────────────────────────
    List<string> Sec4(bool ko, IReadOnlyList<ResolvedModel> usable)
    {
        var L = new List<string>();
        L.Add(ko ? "4. 제품버전 :" : "4. Product Version :");

        foreach (var m in usable.Where(x => !x.FolderFound))
            Warn($"[{(ko ? "KO" : "EN")}] {m.Name} · 폴더 {m.FolderName} 없음 — 4번에서 제외");

        var blocks = ModelResolver.BuildBlocks(usable, _cfg);
        if (blocks.Count == 0)
        {
            Err($"[{(ko ? "KO" : "EN")}] 생성할 제품버전 블록이 없습니다");
            return L;
        }

        int n = 1;
        foreach (var b in blocks)
        {
            L.Add($"    4-{n}) {string.Join(", ", b.Models)}");
            string fw = b.Files.Count > 0
                ? $"{b.FolderName}.zip ({string.Join(", ", b.Files)})"
                : $"{b.FolderName}.zip ()";
            L.Add($"        - F/W : {fw}");
            L.Add($"        - CPU : {b.Cpu}");
            L.Add($"        - BOM : {b.Bom}");
            L.Add($"        - Ver : {b.Version}");
            n++;
        }
        Info($"[{(ko ? "KO" : "EN")}] 제품버전 {blocks.Count}개 블록 생성");
        return L;
    }

    // ══════════════════════════════════════════════════════
    //  6. 제조환경
    // ══════════════════════════════════════════════════════
    List<string> Sec6(
        bool ko,
        SpecKind kind,
        IReadOnlyList<VersionGroup> groups,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<ResolvedModel> usable,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcelModels,
        Func<string, (string? Hw, string? Tests)> reworkFor,
        Dictionary<Para, ParaMode> paras,
        Section6Template t)
    {
        var L = new List<string>();
        L.Add(t.Header);   // "6. 제조환경"

        if (kind == SpecKind.Rework)
        {
            // 6-1) 구미 재작업 공정
            L.Add($"    {t.ReworkHeader}");
            L.AddRange(Rework61(ko, registered, allExcelModels, reworkFor, paras));
            // 6-2) JIG 사용 공정
            L.Add($"    {t.JigHeader}");
            L.AddRange(JigProcess(ko, groups, registered, usable, allExcelModels, paras, t, indent2: true));
        }
        else
        {
            L.AddRange(JigProcess(ko, groups, registered, usable, allExcelModels, paras, t, indent2: false));
        }
        return L;
    }

    // ── 6-1) 구미 재작업 공정 ────────────────────────
    List<string> Rework61(
        bool ko,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcel,
        Func<string, (string? Hw, string? Tests)> reworkFor,
        Dictionary<Para, ParaMode> paras)
    {
        var L = new List<string>();
        var mode = paras.TryGetValue(Para.ReworkGumi, out var mm) ? mm : ParaMode.RegisteredOnly;

        // 후보 모델 풀을 모드에 따라 구성한다.
        //  - 등록만: 등록된 모델
        //  - 전부:   화이트리스트에 있는 모든 모델 (엑셀 순서)
        //  - 관련그룹: 등록 모델이 속한 "테스트 그룹"(동일 테스트 원본)에 속한 모델
        //    → 먼저 전체 후보의 테스트를 훑어, 등록 모델과 같은 테스트를 쓰는 모델을 통째로
        List<string> candidateNames = mode switch
        {
            ParaMode.RegisteredOnly => registered.Select(r => r.Name).ToList(),
            _ => BuildReworkCandidates(allExcel, registered)   // All · RelatedGroups 공통 후보
        };

        // (테스트 원본 → 모델들) 그룹핑. 입력 순서 유지.
        var byTests = new Dictionary<string, List<string>>();
        var order = new List<string>();
        var testsOf = new Dictionary<string, string>();   // 모델 → 테스트 원본

        foreach (var name in candidateNames)
        {
            var (_, tests) = reworkFor(name);
            if (string.IsNullOrWhiteSpace(tests)) continue;
            testsOf[name] = tests;
            if (!byTests.ContainsKey(tests)) { byTests[tests] = new(); order.Add(tests); }
            byTests[tests].Add(name);
        }

        // 관련그룹: 등록 모델이 실제로 쓰는 테스트 그룹만 남긴다.
        if (mode == ParaMode.RelatedGroups)
        {
            var keepTests = new HashSet<string>(
                registered.Select(r => r.Name)
                          .Where(testsOf.ContainsKey)
                          .Select(n => testsOf[n]));
            order = order.Where(keepTests.Contains).ToList();
        }

        if (order.Count == 0)
        {
            Warn($"[{(ko ? "KO" : "EN")}] 6-1 구미 재작업 테스트 항목을 재작업 시트에서 찾지 못했습니다");
            return L;
        }

        foreach (var raw in order)
        {
            var models = byTests[raw];
            L.Add($"      - {string.Join(", ", models)}");
            L.AddRange(RenumberReworkTests(raw, ko));
        }
        return L;
    }

    /// <summary>
    /// 6-1 후보 모델 이름 목록을 만든다.
    /// 등록 모델 먼저(등록 순서 보존), 이어서 화이트리스트에 든 엑셀 모델. 중복 제거.
    /// </summary>
    List<string> BuildReworkCandidates(
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcel,
        IReadOnlyList<ResolvedModel> registered)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();

        // 등록 모델 먼저 (순서 보존)
        foreach (var r in registered)
            if (seen.Add(r.Name)) list.Add(r.Name);

        // 엑셀에서 화이트리스트에 든 모델 추가
        foreach (var (name, _, _) in allExcel)
            if (IsKnown(name) && seen.Add(name)) list.Add(name);

        return list;
    }

    /// <summary>
    /// 테스트 항목의 번호를 떼고, 맨 앞에 "F/W Copy" 를 넣어 1) 2) 3) 재번호.
    /// 영문(ko=false)이면 번역 사전으로 각 줄을 영문으로 바꾼다.
    /// </summary>
    List<string> RenumberReworkTests(string raw, bool ko)
    {
        // 원본 줄 파싱 (번호 제거, 빈 줄 제외)
        var lines = new List<string>();
        foreach (var line in raw.Replace("\r\n", "\n").Split('\n'))
        {
            var t = line.Trim();
            if (t.Length == 0) continue;
            t = Regex.Replace(t, @"^\d+\s*[.)]\s*", "");   // 앞 번호 제거
            lines.Add(t);
        }

        // 맨 앞 F/W Copy 보장 (원본에 이미 있으면 중복 방지)
        if (lines.Count == 0 || !string.Equals(lines[0], "F/W Copy", StringComparison.OrdinalIgnoreCase))
            lines.Insert(0, "F/W Copy");

        // 영문이면 사전 번역
        if (!ko)
            lines = lines.Select(TranslateReworkLine).ToList();

        var outp = new List<string>();
        for (int i = 0; i < lines.Count; i++)
            outp.Add($"        {i + 1}) {lines[i]}");
        return outp;
    }

    /// <summary>한글 테스트 한 줄 → 영문. 사전에 없으면 원문 유지 + 경고.</summary>
    string TranslateReworkLine(string ko)
    {
        var hit = _cfg.ReworkTestDict.FirstOrDefault(p =>
            string.Equals(NormalizeTest(p.Ko), NormalizeTest(ko), StringComparison.Ordinal));
        if (hit is not null && !string.IsNullOrWhiteSpace(hit.En))
            return hit.En;

        Warn($"[EN] 재작업 테스트 번역 없음: \"{ko}\" — 설정에서 추가하세요");
        return ko;   // 미매칭 시 원문 유지
    }

    /// <summary>공백·따옴표 차이를 흡수해 매칭 정확도를 높인다.</summary>
    static string NormalizeTest(string s) =>
        Regex.Replace(s.Replace('\u2018', '\'').Replace('\u2019', '\'')
                       .Replace('\u201C', '"').Replace('\u201D', '"'),
                      @"\s+", " ").Trim();

    // ── 6-2) / 양산 : JIG 사용 공정 ──────────────────
    List<string> JigProcess(
        bool ko,
        IReadOnlyList<VersionGroup> groups,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<ResolvedModel> usable,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcelModels,
        Dictionary<Para, ParaMode> paras,
        Section6Template t,
        bool indent2)
    {
        // indent2 : 재작업 시방일 때 6-2 아래라 한 단계 더 들여씀
        string pad = indent2 ? "    " : "";
        var L = new List<string>();

        // - Copy 공정에서 Region/Theme 적용
        L.Add($"{pad}    {t.CopyIntro}");
        L.Add($"{pad}      {t.CmdLine}");
        // 단락 ① CMD REGION/THEME
        L.AddRange(Indent(ParaCmd(ko, registered, allExcelModels, paras), pad));
        L.Add($"{pad}      {t.BootLine}");

        // - 검사들
        L.Add($"{pad}    {t.TestsIntro}");
        foreach (var line in t.TestsBody.Replace("\r\n", "\n").Split('\n'))
            L.Add($"{pad}      {line}");

        // - LCD 결과
        L.Add($"{pad}    {t.LcdIntro}");
        // 1) Wi-Fi/Buzzer 표시
        L.Add($"{pad}      {t.LcdWifiIntro}");
        L.AddRange(Indent(ParaHw(ko, registered, paras), pad));
        // 2)~8) 고정, {SW} 자리에 S/W Version
        var sw = SwVersionLines(ko, groups, usable, t);
        foreach (var line in t.LcdMiddle.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Trim() == "{SW}") { foreach (var s in sw) L.Add($"{pad}{s}"); }
            else L.Add($"{pad}      {line}");
        }
        // 9) Region, Language, Theme
        L.Add($"{pad}      {t.LcdRegionIntro}");
        L.AddRange(Indent(ParaRegion(ko, registered, allExcelModels, paras), pad));

        return L;
    }

    static IEnumerable<string> Indent(IEnumerable<string> lines, string pad) =>
        pad.Length == 0 ? lines : lines.Select(l => pad + l);

    // ── S/W Version 줄 (단일/다중 분기) ──────────────
    List<string> SwVersionLines(
        bool ko,
        IReadOnlyList<VersionGroup> groups,
        IReadOnlyList<ResolvedModel> usable,
        Section6Template t)
    {
        // 버전별로 모델 묶기 (등록 순서 유지)
        var order = new List<string>();
        var byVer = new Dictionary<string, List<string>>();
        foreach (var m in usable)
        {
            if (!byVer.ContainsKey(m.Version)) { byVer[m.Version] = new(); order.Add(m.Version); }
            byVer[m.Version].Add(m.Name);
        }

        var L = new List<string>();
        if (order.Count == 0)
        {
            L.Add($"      {t.SwVersionLabel} : ? {(ko ? "확인" : "")}".TrimEnd());
            return L;
        }
        if (order.Count == 1)
        {
            string v = order[0];
            L.Add(ko ? $"      {t.SwVersionLabel} : {v} 확인"
                     : $"      {t.SwVersionLabel}: {v}");
            return L;
        }
        // 다중 버전 → 하위 항목으로 확장
        L.Add($"      {t.SwVersionLabel}");
        foreach (var v in order)
            L.Add($"          - {v} : {string.Join(", ", byVer[v])}");
        return L;
    }

    // ══════════════════════════════════════════════════════
    //  단락별 모델 나열 (ParaMode 적용)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// ParaMode 에 따라 모델 풀을 결정한다.
    /// - All: 엑셀 전부 (allExcel 이 있으면 그것, 없으면 등록 모델)
    /// - RegisteredOnly: 등록 모델만
    /// - RelatedGroups: 등록 모델이 속한 (Region,Theme) 그룹 통째
    /// keySelector 가 null 이면 그룹 개념이 없으므로 등록/전부만 구분.
    /// </summary>
    List<ResolvedModel> FilterByPara(
        IReadOnlyList<ResolvedModel> registered,
        Para para,
        Dictionary<Para, ParaMode> paras,
        Func<ResolvedModel, object?>? keySelector,
        IReadOnlyList<(string Model, string? Region, string? Theme)>? allExcel = null)
    {
        var mode = paras.TryGetValue(para, out var m) ? m : ParaMode.RelatedGroups;

        if (mode == ParaMode.RegisteredOnly)
            return registered.ToList();

        if (mode == ParaMode.All)
        {
            if (allExcel is null) return registered.ToList();
            // 엑셀 전체를 ResolvedModel 로 감싼다 (등록 순서 뒤에 나머지)
            // 등록 모델은 항상 포함하고, 엑셀에서 새로 끌어온 항목은
            // 화이트리스트에 있는 것만 통과시킨다 (비-모델 문구 배제).
            var byName = registered.ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);
            var list = new List<ResolvedModel>();
            int order = 0;
            foreach (var (name, region, theme) in allExcel)
            {
                if (byName.TryGetValue(name, out var r)) { list.Add(r); continue; }
                if (!IsKnown(name)) continue;   // 화이트리스트 밖 → 조회/출력 생략
                list.Add(new ResolvedModel { Name = name, Region = region, Theme = theme, Order = 10000 + order++ });
            }
            return list;
        }

        // RelatedGroups
        if (keySelector is null) return registered.ToList();
        var keys = new HashSet<object?>(registered.Select(keySelector));
        var pool = new List<ResolvedModel>();
        if (allExcel is not null)
        {
            var byName = registered.ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);
            foreach (var (name, region, theme) in allExcel)
            {
                bool isRegistered = byName.TryGetValue(name, out var r);
                if (!isRegistered && !IsKnown(name)) continue;   // 화이트리스트 밖 → 생략
                var rm = isRegistered ? r!
                    : new ResolvedModel { Name = name, Region = region, Theme = theme };
                if (keys.Contains(keySelector(rm))) pool.Add(rm);
            }
        }
        else
        {
            pool = registered.Where(x => keys.Contains(keySelector(x))).ToList();
        }
        return pool;
    }

    /// <summary>① CMD 전송 : (Region,Theme) 그룹별로 -> TESTMODE REGION:X THEME:Y</summary>
    List<string> ParaCmd(
        bool ko,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcel,
        Dictionary<Para, ParaMode> paras)
    {
        var pool = FilterByPara(registered, Para.CmdRegionTheme, paras,
            m => (m.Region, m.Theme), allExcel);

        var order = new List<(string?, string?)>();
        var groups = new Dictionary<(string?, string?), List<string>>();
        foreach (var m in pool)
        {
            if (m.Region is null) { Warn($"[{(ko ? "KO" : "EN")}] {m.Name} · Region 없음 — CMD 나열 제외"); continue; }
            var k = (m.Region, m.Theme);
            if (!groups.ContainsKey(k)) { groups[k] = new(); order.Add(k); }
            groups[k].Add(m.Name);
        }

        var L = new List<string>();
        foreach (var (region, theme) in order)
        {
            L.Add($"          - {string.Join(", ", groups[(region, theme)])}");
            L.Add($"            -> TESTMODE REGION:{region} THEME:{theme ?? "white"}");
        }
        return L;
    }

    /// <summary>② HW Function : Wi-Fi 모델, Buzzer 모델만 나열. '-' 는 제외 문구.</summary>
    List<string> ParaHw(
        bool ko,
        IReadOnlyList<ResolvedModel> registered,
        Dictionary<Para, ParaMode> paras)
    {
        // HW 는 그룹 키가 HwFunction. 관련그룹 = 같은 HW.
        var pool = FilterByPara(registered, Para.HwFunction, paras, m => m.HwFunction);

        var wifi = pool.Where(m => string.Equals(m.HwFunction, "Wi-Fi", StringComparison.OrdinalIgnoreCase))
                       .Select(m => m.Name).ToList();
        var buzzer = pool.Where(m => string.Equals(m.HwFunction, "Buzzer", StringComparison.OrdinalIgnoreCase))
                         .Select(m => m.Name).ToList();

        var L = new List<string>();
        if (ko)
        {
            if (wifi.Count > 0) L.Add($"          - 'Wi-Fi' 표시 모델 : {string.Join(", ", wifi)}");
            if (buzzer.Count > 0) L.Add($"          - 'Buzzer' 표시 모델 : {string.Join(", ", buzzer)}");
            L.Add("          - '-' 표시 모델 : 'Wi-Fi', 'Buzzer' 표시 제외 모델");
        }
        else
        {
            if (wifi.Count > 0) L.Add($"          - Models with \u201cWi-Fi\u201d displayed : {string.Join(", ", wifi)}");
            if (buzzer.Count > 0) L.Add($"          - Models with \u201cBuzzer\u201d displayed : {string.Join(", ", buzzer)}");
            L.Add("          - Models with \u201c\u2013\u201d displayed : models without \u201cWi-Fi\u201d or \u201cBuzzer\u201d indicators.");
        }
        return L;
    }

    /// <summary>③ Region, Language, Theme : 향(向) 라벨 + 모델 나열.</summary>
    List<string> ParaRegion(
        bool ko,
        IReadOnlyList<ResolvedModel> registered,
        IReadOnlyList<(string Model, string? Region, string? Theme)> allExcel,
        Dictionary<Para, ParaMode> paras)
    {
        var pool = FilterByPara(registered, Para.RegionLangTheme, paras, m => m.Region, allExcel);

        var order = new List<string>();
        var groups = new Dictionary<string, List<ResolvedModel>>();
        foreach (var m in pool)
        {
            if (m.Region is null) continue;
            if (!groups.ContainsKey(m.Region)) { groups[m.Region] = new(); order.Add(m.Region); }
            groups[m.Region].Add(m);
        }

        var L = new List<string>();
        foreach (var region in order)
        {
            var label = _cfg.RegionLabels.FirstOrDefault(x =>
                string.Equals(x.Region, region, StringComparison.OrdinalIgnoreCase));
            string name = label is null ? region : (ko ? label.Ko : label.En);
            string lang = label?.EffectiveLanguage
                ?? (string.Equals(region, "KOR", StringComparison.OrdinalIgnoreCase) ? "KOR" : "ENG");
            string theme = groups[region].FirstOrDefault(m => m.Theme is not null)?.Theme ?? "white";

            if (label is null)
                Warn($"[{(ko ? "KO" : "EN")}] Region '{region}' 라벨 매핑 없음 — 톱니바퀴에서 추가하세요");

            L.Add($"        - {name}(Region : {region}, Language : {lang}, Theme : {theme})");
            L.Add($"          {string.Join(", ", groups[region].Select(m => m.Name))}");
        }
        return L;
    }
}
