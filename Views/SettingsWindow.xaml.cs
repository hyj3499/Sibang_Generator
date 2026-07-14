using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Sibang_generator.Models;

namespace Sibang_generator.Views;

/// <summary>
/// 톱니바퀴 설정창.
/// - Region 라벨 매핑 (DataGrid, 편집 가능)
/// - 상수 섹션 0,2,3,5,7,8,9 한/영 기본 본문
/// - 6번 고정 골격 한/영 문구
///
/// 저장을 누르면 편집 내용을 Config 에 반영한다.
/// (경로/열 매핑은 메인 화면에서 직접 Config 에 양방향 바인딩되어 있으므로
///  여기서는 다루지 않는다.)
///
/// DataContext = Config 이므로 상수/골격 탭의 TextBox 들은 양방향 바인딩으로
/// 자동 반영된다. RegionGrid 만 ObservableCollection 으로 따로 관리한다.
/// </summary>
public partial class SettingsWindow : Window
{
    readonly AppConfig _config;
    readonly ObservableCollection<RegionLabel> _regions;
    readonly ObservableCollection<ReworkTestPair> _reworkPairs;

    public SettingsWindow(AppConfig config)
    {
        InitializeComponent();

        _config = config;
        DataContext = _config;   // 상수 섹션 · 6번 골격 TextBox 양방향 바인딩

        // Region 라벨은 복사본으로 편집 → 저장 시 반영, 취소 시 원본 유지
        _regions = new ObservableCollection<RegionLabel>(
            _config.RegionLabels.Select(Clone));
        RegionGrid.ItemsSource = _regions;

        // 재작업 테스트 번역 사전도 복사본으로 편집
        _reworkPairs = new ObservableCollection<ReworkTestPair>(
            _config.ReworkTestDict.Select(Clone));
        ReworkGrid.ItemsSource = _reworkPairs;
    }

    static RegionLabel Clone(RegionLabel r) => new()
    {
        Region = r.Region,
        Ko = r.Ko,
        En = r.En,
        Language = r.Language
    };

    static ReworkTestPair Clone(ReworkTestPair p) => new() { Ko = p.Ko, En = p.En };

    // ── Region 행 삭제 ──
    void DeleteRegion_Click(object sender, RoutedEventArgs e)
    {
        if (RegionGrid.SelectedItem is RegionLabel r)
            _regions.Remove(r);
    }

    // ── Region 기본값 복원 ──
    void ResetRegion_Click(object sender, RoutedEventArgs e)
    {
        var ok = MessageBox.Show(
            "Region 라벨을 기본값으로 되돌립니다. 진행할까요?",
            "기본값 복원", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        _regions.Clear();
        foreach (var r in AppConfig.DefaultRegionLabels())
            _regions.Add(r);
    }

    // ── 재작업 테스트 번역 행 삭제 ──
    void DeleteRework_Click(object sender, RoutedEventArgs e)
    {
        if (ReworkGrid.SelectedItem is ReworkTestPair p)
            _reworkPairs.Remove(p);
    }

    // ── 재작업 테스트 번역 기본값 복원 ──
    void ResetRework_Click(object sender, RoutedEventArgs e)
    {
        var ok = MessageBox.Show(
            "재작업 테스트 번역표를 기본값으로 되돌립니다. 진행할까요?",
            "기본값 복원", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        _reworkPairs.Clear();
        foreach (var p in ReworkTestPair.Defaults())
            _reworkPairs.Add(p);
    }

    // ── 저장 ──
    void Save_Click(object sender, RoutedEventArgs e)
    {
        // DataGrid 편집 중인 셀을 확정
        RegionGrid.CommitEdit(DataGridEditingUnit.Row, true);
        ReworkGrid.CommitEdit(DataGridEditingUnit.Row, true);

        // 빈 행 제거 후 Config 에 반영
        var cleaned = _regions
            .Where(r => !string.IsNullOrWhiteSpace(r.Region))
            .Select(r => new RegionLabel
            {
                Region = r.Region.Trim(),
                Ko = (r.Ko ?? "").Trim(),
                En = (r.En ?? "").Trim(),
                Language = (r.Language ?? "").Trim()
            })
            .ToList();

        _config.RegionLabels = cleaned.Count > 0
            ? cleaned
            : AppConfig.DefaultRegionLabels();

        // 재작업 테스트 번역표 반영 (한글이 있는 행만)
        _config.ReworkTestDict = _reworkPairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Ko))
            .Select(p => new ReworkTestPair
            {
                Ko = p.Ko.Trim(),
                En = (p.En ?? "").Trim()
            })
            .ToList();

        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
