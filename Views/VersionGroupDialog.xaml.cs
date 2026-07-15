using System.Windows;
using System.Windows.Controls;
using Sibang_generator.Models;
using Sibang_generator.Services;

namespace Sibang_generator.Views;

/// <summary>
/// 버전 그룹 추가/편집 대화상자.
///
/// [모델 추가] 모드:
///  - 콤보에는 "아직 등록되지 않은" 버전만 보인다.
///    (예: 1.00.7a 를 이미 등록했으면 1.00.8a 부터 보인다)
///  - 콤보에서 버전을 바꾸면, 그 버전에 이미 입력해 둔 모델이 있으면 텍스트에 표시하고
///    없으면 텍스트를 비운다.
///  - 목록에 없는 버전은 직접 타이핑도 가능하다.
///
/// [모델 편집] 모드:
///  - 편집 대상 버전은 콤보에 그대로 보이고, 그 모델이 텍스트에 채워진다.
///
/// 동일 모델을 서로 다른 버전으로 등록하는 것을 허용한다(모델 중복 검사 없음).
/// </summary>
public partial class VersionGroupDialog : Window
{
    readonly Dictionary<string, VersionGroup> _existing;   // 버전 → 기존 그룹
    readonly string? _editingVersion;                       // 편집 대상 (신규면 null)

    public VersionGroup? Result { get; private set; }

    public VersionGroupDialog(
        IEnumerable<string> availableVersions,
        IEnumerable<VersionGroup> existingGroups,
        VersionGroup? editing = null)
    {
        InitializeComponent();

        _existing = existingGroups.ToDictionary(g => g.Version, g => g, StringComparer.Ordinal);
        _editingVersion = editing?.Version;

        // 콤보 선택지 구성:
        //  - 편집 대상 버전은 포함
        //  - 이미 등록된 다른 버전은 제외 (추가 시 중복 버전 숨김)
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var choices = new List<string>();
        foreach (var v in availableVersions)
        {
            if (_existing.ContainsKey(v) && v != _editingVersion) continue;  // 이미 등록 → 숨김
            if (seen.Add(v)) choices.Add(v);
        }
        // 편집 대상이 폴더 목록에 없더라도 반드시 보이게
        if (_editingVersion is not null && seen.Add(_editingVersion))
            choices.Insert(0, _editingVersion);

        CbVersion.ItemsSource = choices;

        VerHint.Text = choices.Count > 0
            ? "이미 등록한 버전은 목록에서 빠집니다. 목록에 없으면 직접 입력하세요."
            : "선택 가능한 새 버전이 없습니다. 버전을 직접 입력하세요 (예: 1.00.8a).";

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
            // 첫 버전에 기존 입력이 있으면 표시
            LoadModelsFor(choices[0]);
        }
    }

    string CurrentVersion => (CbVersion.Text ?? "").Trim();

    /// <summary>해당 버전에 이미 등록된 모델이 있으면 텍스트에 넣고, 없으면 비운다.</summary>
    void LoadModelsFor(string version)
    {
        if (_existing.TryGetValue(version, out var grp))
            TbModels.Text = grp.RawModels;
        else
            TbModels.Text = "";
    }

    void Ver_Changed(object s, SelectionChangedEventArgs e)
    {
        // 콤보에서 명시적으로 버전을 고른 경우에만 모델 텍스트를 그 버전 기준으로 바꾼다.
        if (CbVersion.SelectedItem is string picked)
        {
            // 편집 모드에서 편집 대상 버전을 다시 고른 게 아니면 로드
            LoadModelsFor(picked);

            if (_existing.ContainsKey(picked))
            {
                Head.Text = $"모델 편집 · {picked}";
                Title = "모델 편집";
            }
            else
            {
                Head.Text = "모델 추가";
                Title = "모델 추가";
            }
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

        // 동일 모델을 다른 버전으로 등록하는 것을 허용하므로 모델 중복 검사는 하지 않는다.
        bool editingThis = _existing.ContainsKey(ver) &&
                           (ver == _editingVersion || CbVersion.SelectedItem as string == ver);

        if (string.IsNullOrWhiteSpace(ver))
            Warn.Text = "버전을 입력하세요";
        else if (models.Count == 0)
            Warn.Text = "모델을 입력하세요";
        else
            Warn.Text = editingThis ? $"편집 중 · {models.Count}개 모델"
                                    : $"{models.Count}개 모델";

        OkBtn.IsEnabled = !string.IsNullOrWhiteSpace(ver) && models.Count > 0;
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
        // 같은 버전을 편집하는 경우 기존 BOM 직접지정 보존
        if (_existing.TryGetValue(CurrentVersion, out var old))
            Result.BomOverrides = old.BomOverrides;
        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
