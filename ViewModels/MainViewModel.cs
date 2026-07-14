using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Sibang_generator.Models;
using Sibang_generator.Services;

namespace Sibang_generator.ViewModels;

/// <summary>
/// 한 화면에 모든 설정을 펼치고 [시방 제작] 한 번으로 미리보기를 내는 방식.
/// 단계 이동이 없다.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    public AppConfig Config { get; private set; }

    public MainViewModel()
    {
        Config = ConfigStore.Load();
        // 6번 단락 옵션 기본 구성
        InitParaOptions();

        AddGroupCommand     = new RelayCommand(AddGroup);
        EditGroupCommand    = new RelayCommand(_ => EditGroup(), _ => SelectedGroup is not null);
        RemoveGroupCommand  = new RelayCommand(_ => RemoveGroup(), _ => SelectedGroup is not null);
        BrowseFirmwareCommand = new RelayCommand(BrowseFirmware);
        BrowseExcelCommand    = new RelayCommand(BrowseExcel);
        AttachConstTxtCommand = new RelayCommand(AttachConstTxt);
        ClearConstTxtCommand  = new RelayCommand(_ => ClearConstTxt(), _ => ConstFromFile);
        RefreshLookupCommand  = new RelayCommand(_ => RefreshLookup());
        GenerateCommand       = new RelayCommand(_ => Generate());
        SaveCommand           = new RelayCommand(_ => SaveOutput(), _ => _lastResult is not null);
        OpenSettingsCommand   = new RelayCommand(OpenSettings);
        OverrideBomCommand    = new RelayCommand(OverrideBom);
        SaveConfigCommand     = new RelayCommand(_ => PersistConfig());
    }

    // ══════════ ① 시방 종류 ══════════
    bool _isRework;
    public bool IsRework { get => _isRework; set { if (Set(ref _isRework, value)) SyncParaOptions(); } }
    public bool IsMass { get => !_isRework; set { IsRework = !value; OnPropertyChanged(nameof(IsRework)); OnPropertyChanged(nameof(IsMass)); } }
    public SpecKind Kind => IsRework ? SpecKind.Rework : SpecKind.MassProduction;

    // 옛 시방 txt (0,2,3,5,7,8,9 상수 출처)
    bool _constFromFile;
    public bool ConstFromFile { get => _constFromFile; private set => Set(ref _constFromFile, value); }
    string? _constTxtName;
    public string ConstTxtStatus => ConstFromFile ? $"첨부됨: {_constTxtName}" : "상수 기본값 사용 (미첨부)";
    ConstSections? _importedKo, _importedEn;

    // ══════════ 6번 단락 옵션 ══════════
    public ObservableCollection<ParaOption> ParaOptions { get; } = new();

    void InitParaOptions()
    {
        ParaOptions.Clear();
        ParaOptions.Add(new ParaOption { Para = Para.CmdRegionTheme,  Mode = ParaMode.RelatedGroups });
        ParaOptions.Add(new ParaOption { Para = Para.HwFunction,      Mode = ParaMode.RelatedGroups });
        ParaOptions.Add(new ParaOption { Para = Para.RegionLangTheme, Mode = ParaMode.RelatedGroups });
        SyncParaOptions();
    }

    /// <summary>재작업 시방이면 6-1 단락 옵션을 추가, 아니면 제거.</summary>
    void SyncParaOptions()
    {
        var gumi = ParaOptions.FirstOrDefault(p => p.Para == Para.ReworkGumi);
        if (IsRework && gumi is null)
            ParaOptions.Insert(0, new ParaOption { Para = Para.ReworkGumi, Mode = ParaMode.RegisteredOnly });
        else if (!IsRework && gumi is not null)
            ParaOptions.Remove(gumi);
    }

    public static ParaMode[] AllParaModes => new[] { ParaMode.All, ParaMode.RegisteredOnly, ParaMode.RelatedGroups };

    // ══════════ 모델 · 버전 그룹 ══════════
    public ObservableCollection<VersionGroup> Groups { get; } = new();
    VersionGroup? _selectedGroup;
    public VersionGroup? SelectedGroup { get => _selectedGroup; set => Set(ref _selectedGroup, value); }

    public ObservableCollection<ResolvedModel> Resolved { get; } = new();

    // ══════════ 미리보기 ══════════
    bool _showPreview;
    public bool ShowPreview { get => _showPreview; set => Set(ref _showPreview, value); }

    // 한글 + 영문을 세로로 이어붙인, 저장되는 형태 그대로의 본문
    string _previewText = "";
    public string PreviewText { get => _previewText; set => Set(ref _previewText, value); }

    string _logText = "";
    public string LogText { get => _logText; set => Set(ref _logText, value); }

    // 오류/경고 버블
    public ObservableCollection<Issue> Alerts { get; } = new();

    GenerationResult? _lastResult;

    // ══════════ Commands ══════════
    public RelayCommand AddGroupCommand { get; }
    public RelayCommand EditGroupCommand { get; }
    public RelayCommand RemoveGroupCommand { get; }
    public RelayCommand BrowseFirmwareCommand { get; }
    public RelayCommand BrowseExcelCommand { get; }
    public RelayCommand AttachConstTxtCommand { get; }
    public RelayCommand ClearConstTxtCommand { get; }
    public RelayCommand RefreshLookupCommand { get; }
    public RelayCommand GenerateCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OverrideBomCommand { get; }
    public RelayCommand SaveConfigCommand { get; }

    // ── 경로 브라우즈 ──
    void BrowseFirmware(object? _)
    {
        var dlg = new OpenFolderDialog { Title = "펌웨어 루트 폴더 선택" };
        if (dlg.ShowDialog() == true) { Config.FirmwareRoot = dlg.FolderName; OnPropertyChanged(nameof(Config)); PersistConfig(); }
    }

    void BrowseExcel(object? _)
    {
        var dlg = new OpenFileDialog { Title = "BOM 엑셀 선택", Filter = "Excel|*.xlsx;*.xlsm|모든 파일|*.*" };
        if (dlg.ShowDialog() == true) { Config.ExcelPath = dlg.FileName; OnPropertyChanged(nameof(Config)); PersistConfig(); }
    }

    // ── 옛 시방 txt 첨부 ──
    void AttachConstTxt(object? _)
    {
        var dlg = new OpenFileDialog { Title = "옛 시방 txt 선택 (0,2,3,5,7,8,9 상수 출처)", Filter = "텍스트|*.txt|모든 파일|*.*" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var text = ConstSectionParser.ReadTextAuto(dlg.FileName);
            var (ko, en) = ConstSectionParser.Parse(text);
            _importedKo = ko; _importedEn = en;
            _constTxtName = Path.GetFileName(dlg.FileName);
            ConstFromFile = true;
            OnPropertyChanged(nameof(ConstTxtStatus));
            MessageBox.Show(
                $"상수 섹션을 가져왔습니다.\n한글: 0,2,3,5,7,8,9\n영문: {(en is null ? "없음(기본값 사용)" : "0,2,3,5,7,8,9")}",
                "첨부 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"파싱 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void ClearConstTxt()
    {
        _importedKo = _importedEn = null;
        _constTxtName = null;
        ConstFromFile = false;
        OnPropertyChanged(nameof(ConstTxtStatus));
    }

    // ── 버전 그룹 ──
    void AddGroup(object? _)
    {
        var fw = new FirmwareScanner(Config);
        var dlg = new Views.VersionGroupDialog(
            fw.DiscoverVersions(),
            Groups)   // 기존 그룹 전체 → 콤보에 표시 + 선택 시 편집
        { Owner = Application.Current.MainWindow };

        if (dlg.ShowDialog() == true && dlg.Result is not null)
            ApplyGroupResult(dlg.Result);
    }

    void EditGroup()
    {
        if (SelectedGroup is null) return;
        var fw = new FirmwareScanner(Config);
        var dlg = new Views.VersionGroupDialog(
            fw.DiscoverVersions(),
            Groups,
            SelectedGroup)
        { Owner = Application.Current.MainWindow };

        if (dlg.ShowDialog() == true && dlg.Result is not null)
            ApplyGroupResult(dlg.Result);
    }

    /// <summary>
    /// 다이얼로그 결과를 반영한다.
    /// 같은 버전이 이미 있으면 그 자리를 교체(편집), 없으면 새로 추가.
    /// </summary>
    void ApplyGroupResult(VersionGroup result)
    {
        var existing = Groups.FirstOrDefault(g => g.Version == result.Version);
        if (existing is not null)
        {
            int i = Groups.IndexOf(existing);
            Groups[i] = result;
        }
        else
        {
            Groups.Add(result);
        }
        SelectedGroup = result;
        RefreshLookup();
    }

    void RemoveGroup()
    {
        if (SelectedGroup is null) return;
        Groups.Remove(SelectedGroup);
        RefreshLookup();
    }

    // ── 조회 새로고침 ──
    public void RefreshLookup()
    {
        Resolved.Clear();
        BomLookup? bom = null;
        ReworkLookup? rw = null;
        try
        {
            bom = new BomLookup(Config);
            rw = new ReworkLookup(Config);
            var fw = new FirmwareScanner(Config);
            var list = ModelResolver.Resolve(Groups, bom, rw, fw);
            foreach (var m in list) Resolved.Add(m);
        }
        finally
        {
            bom?.Dispose();
            rw?.Dispose();
        }
        SaveCommand.RaiseCanExecuteChanged();
    }

    // ── BOM 직접 지정 ──
    void OverrideBom(object? param)
    {
        if (param is not ResolvedModel rm) return;
        var fw = new FirmwareScanner(Config);
        var dlg = new Views.BomOverrideDialog(rm, fw.DiscoverBoms(rm.Version))
        { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() == true)
        {
            var grp = Groups.FirstOrDefault(g => g.Models.Contains(rm.Name));
            if (grp is not null && dlg.SelectedBom is not null)
                grp.BomOverrides[rm.Name] = dlg.SelectedBom;
            RefreshLookup();
        }
    }

    // ── 시방 제작 ──
    void Generate()
    {
        if (Groups.Count == 0)
        {
            MessageBox.Show("등록된 모델이 없습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        RefreshLookup();

        // 엑셀 전체 모델 (전부 파싱 옵션용)
        var allExcel = new List<(string, string?, string?)>();
        BomLookup? bom = null; ReworkLookup? rw = null;
        try
        {
            bom = new BomLookup(Config);
            if (bom.Ready) allExcel = bom.All();
            rw = new ReworkLookup(Config);
            var rwLocal = rw;

            (string?, string?) ReworkFor(string name) =>
                rwLocal.Ready ? rwLocal.Find(name) : (null, null);

            var ko = ConstFromFile && _importedKo is not null ? _importedKo : Config.Ko;
            var en = ConstFromFile && _importedEn is not null ? _importedEn
                     : ConstFromFile && _importedKo is not null ? Config.En  // 영문 미첨부 → 기본 영문
                     : Config.En;

            var gen = new SpecGenerator(Config);
            _lastResult = gen.Generate(
                Kind, Groups.ToList(), Resolved.ToList(),
                allExcel, ReworkFor, ParaOptions.ToList(), ko, en);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"생성 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        finally
        {
            bom?.Dispose();
            rw?.Dispose();
        }

        UpdatePreviewText();
        LogText = SpecWriter.BuildLog(_lastResult, Groups.ToList());

        // 오류/경고 버블 갱신
        Alerts.Clear();
        int errs = _lastResult.ErrorCount, warns = _lastResult.WarnCount;
        Alerts.Add(new Issue { Sev = errs > 0 ? Severity.Error : Severity.Pass,
            Msg = errs > 0 ? $"오류 {errs}" : "오류 없음" });
        Alerts.Add(new Issue { Sev = warns > 0 ? Severity.Warning : Severity.Pass,
            Msg = warns > 0 ? $"경고 {warns}" : "경고 없음" });
        // 개별 오류/경고도 버블로 (최대 12개)
        foreach (var x in _lastResult.Log
                     .Where(i => i.Sev is Severity.Error or Severity.Warning).Take(12))
            Alerts.Add(x);

        ShowPreview = true;
        SaveCommand.RaiseCanExecuteChanged();
    }

    void UpdatePreviewText()
    {
        if (_lastResult is null) return;
        // 저장 형태 그대로: 한글 → 빈 줄 → 영문
        var all = new List<string>();
        all.AddRange(_lastResult.KoLines);
        all.Add(""); all.Add("");
        all.AddRange(_lastResult.EnLines);
        PreviewText = string.Join("\r\n", all);
    }

    // ── 저장 ──
    void SaveOutput()
    {
        if (_lastResult is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "시방 저장",
            Filter = "텍스트|*.txt",
            FileName = $"시방_{DateTime.Now:yyyyMMdd_HHmm}.txt"
        };
        if (dlg.ShowDialog() != true) return;

        var all = new List<string>();
        all.AddRange(_lastResult.KoLines);
        all.Add(""); all.Add("");
        all.AddRange(_lastResult.EnLines);
        SpecWriter.Write(dlg.FileName, all);

        MessageBox.Show("저장했습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── 설정 ──
    void OpenSettings(object? _)
    {
        var dlg = new Views.SettingsWindow(Config) { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() == true)
        {
            PersistConfig();
            RefreshLookup();
        }
    }

    public void PersistConfig() => ConfigStore.Save(Config);
}
