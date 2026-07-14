using System.Windows;
using System.Windows.Controls;
using Sibang_generator.Models;

namespace Sibang_generator.Views;

/// <summary>
/// BOM 조회 실패(또는 병합 셀)한 모델에 대해 사용자가 직접 BOM 을 지정한다.
/// 폴더에서 발견된 후보를 목록으로 보여주고, 직접 타이핑도 가능하다.
/// </summary>
public partial class BomOverrideDialog : Window
{
    readonly ResolvedModel _model;

    public string? SelectedBom { get; private set; }

    public BomOverrideDialog(ResolvedModel model, IEnumerable<string> candidates)
    {
        InitializeComponent();
        _model = model;

        Head.Text = model.Name;
        Sub.Text = $"버전 {model.Version} · 현재 상태: {model.Status}";

        LbBoms.ItemsSource = candidates.ToList();
        TbBom.Text = model.ManualBom ?? model.ExcelBom ?? "";
    }

    void Bom_Changed(object sender, TextChangedEventArgs e) => UpdateHint();
    void Bom_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (LbBoms.SelectedItem is string s) TbBom.Text = s;
    }

    void UpdateHint()
    {
        string bom = (TbBom.Text ?? "").Trim();
        FolderHint.Text = bom.Length == 0
            ? "BOM 을 입력하거나 후보에서 선택하세요."
            : $"폴더명: {bom}_{_model.Version}";
    }

    void Ok_Click(object sender, RoutedEventArgs e)
    {
        SelectedBom = (TbBom.Text ?? "").Trim();
        if (SelectedBom.Length == 0) SelectedBom = null;
        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
