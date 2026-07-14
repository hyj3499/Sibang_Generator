using System.Windows;
using System.Windows.Controls;
using Sibang_generator.Models;
using Sibang_generator.Services;

namespace Sibang_generator.Views;

/// <summary>
/// 버전 그룹 추가/편집 대화상자.
///
/// 콤보박스에는 펌웨어 폴더에서 발견된 모든 버전을 보여준다.
/// - 아직 등록되지 않은 버전을 고르면 → 신규 추가
/// - 이미 등록된 버전을 고르면 → 그 그룹의 모델을 불러와 편집 모드로 전환
///   (모델 텍스트가 자동으로 채워지고, 확인 시 해당 그룹을 갱신)
/// 목록에 없는 버전은 직접 타이핑도 가능하다.
/// </summary>
public partial class VersionGroupDialog : Window
{
    // 이미 등록된 그룹들 (버전 → 그룹). 편집 전환에 사용.
    readonly Dictionary<string, VersionGroup> _existing;
    // 현재 편집 대상 버전 (신규면 null). 중복 검사에서 자기 자신 제외.
    string? _editingVersion;

    public VersionGroup? Result { get; private set; }

    public VersionGroupDialog(
        IEnumerable<string> availableVersions,
        IEnumerable<VersionGroup> existingGroups,
        VersionGroup? editing = null)
    {
        InitializeComponent();

        _existing = existingGroups.ToDictionary(g => g.Version, g => g, StringComparer.Ordinal);
        _editingVersion = editing?.Version;

        // 발견된 버전 + 이미 등록된 버전을 모두 콤보에 (중복 제거, 순서 유지)
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var choices = new List<string>();
        foreach (var v in availableVersions.Concat(_existing.Keys))
            if (seen.Add(v)) choices.Add(v);
        CbVersion.ItemsSource = choices;

        VerHint.Text = choices.Count > 0
            ? "이미 등록된 버전을 고르면 그 그룹을 편집합니다. 목록에 없으면 직접 입력하세요."
            : "펌웨어 폴더를 아직 지정하지 않았습니다. 버전을 직접 입력하세요 (예: 1.00.7a).";

        if (editing is not null)
        {
            Head.Text = $"모델 편집 · {editing.Version}";
            Title = "모델 편집";
            CbVersion.Text = editing.Version;
            TbModels.Text = editing.RawModels;
        }
        else if (choices.Count > 0)
        {
            CbVersion.SelectedIndex = 0;
        }
    }

    string CurrentVersion => (CbVersion.Text ?? "").Trim();

    void Ver_Changed(object s, SelectionChangedEventArgs e)
    {
        // 콤보에서 이미 등록된 버전을 고르면 → 그 그룹을 편집 모드로 로드
        if (CbVersion.SelectedItem is string picked &&
            _existing.TryGetValue(picked, out var grp))
        {
            _editingVersion = picked;
            Head.Text = $"모델 편집 · {picked}";
            Title = "모델 편집";
            if (string.IsNullOrWhiteSpace(TbModels.Text) || TbModels.Text != grp.RawModels)
                TbModels.Text = grp.RawModels;
        }
        else if (CbVersion.SelectedItem is string newVer && !_existing.ContainsKey(newVer))
        {
            // 신규 버전 선택 → 편집 대상 해제 (기존 편집 중이 아니면)
            _editingVersion = null;
            Head.Text = "모델 추가";
            Title = "모델 추가";
        }
        Validate();
    }

    void Ver_Typed(object s, System.Windows.Input.KeyEventArgs e) => Validate();
    void Models_Changed(object s, TextChangedEventArgs e) => Validate();

    void Validate()
    {
        if (!IsLoaded) return;
        string ver = CurrentVersion;
        var models = ModelResolver.ParseModels(TbModels.Text);

        // 다른 그룹(편집 중인 버전 제외)에 이미 쓰인 모델
        var usedModels = new HashSet<string>(
            _existing.Where(kv => kv.Key != _editingVersion)
                     .SelectMany(kv => kv.Value.Models),
            StringComparer.Ordinal);
        var dup = models.Where(usedModels.Contains).ToList();

        bool versionTaken = _existing.ContainsKey(ver) && ver != _editingVersion;

        if (string.IsNullOrWhiteSpace(ver))
            Warn.Text = "버전을 입력하세요";
        else if (dup.Count > 0)
            Warn.Text = $"다른 그룹에 중복: {string.Join(", ", dup.Take(3))}";
        else if (models.Count == 0)
            Warn.Text = "모델을 입력하세요";
        else
            Warn.Text = _existing.ContainsKey(ver) && ver == _editingVersion
                ? $"편집 중 · {models.Count}개 모델"
                : $"{models.Count}개 모델";

        // versionTaken 이어도, 그 버전을 편집 대상으로 삼으면 통과
        OkBtn.IsEnabled =
            !string.IsNullOrWhiteSpace(ver) && dup.Count == 0 && models.Count > 0;
    }

    void Ok_Click(object sender, RoutedEventArgs e)
    {
        var models = ModelResolver.ParseModels(TbModels.Text);
        Result = new VersionGroup
        {
            Version = CurrentVersion,
            RawModels = TbModels.Text.Trim(),
            Models = models
        };
        // 편집 대상 버전을 알려, 뷰모델이 기존 그룹을 교체하도록 한다
        Result.BomOverrides = _editingVersion is not null &&
            _existing.TryGetValue(_editingVersion, out var old)
            ? old.BomOverrides   // 기존 BOM 직접지정 보존
            : Result.BomOverrides;
        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
