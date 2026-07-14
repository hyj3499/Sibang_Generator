using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Sibang_generator.Models;

namespace Sibang_generator.Services;

/// <summary>
/// 기존 시방 txt 첨부 시, 상수 섹션 0,2,3,5,7,8,9 를 그대로 파싱한다.
///
/// - 한글 구간과 영문 구간을 나눈다. 영문 구간의 시작은 "0. Purpose" 키워드.
///   (한글 구간의 0번은 "0. 시방목적")
/// - 각 구간에서 최상위 번호 헤더(0. / 2. / 3. / 5. / 7. / 8. / 9.)를 찾아
///   다음 최상위 헤더 직전까지를 한 섹션으로 잘라낸다.
/// - 1, 4, 6 은 모델에 따라 생성되므로 무시한다.
/// </summary>
public static class ConstSectionParser
{
    static readonly Regex RxTopHead = new(@"^\s*(\d+)\s*[.)]", RegexOptions.Compiled);
    // 영문 구간 시작: "0. Purpose"
    static readonly Regex RxEnStart = new(@"^\s*0\s*[.)]\s*Purpose", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string ReadTextAuto(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.GetEncoding(949).GetString(bytes); }
    }

    /// <summary>
    /// txt 전체 → (한글 상수섹션, 영문 상수섹션).
    /// 영문 구간이 없으면 En 은 null.
    /// </summary>
    public static (ConstSections Ko, ConstSections? En) Parse(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');

        // 영문 구간 시작 줄 찾기 ("0. Purpose")
        int enStart = -1;
        for (int i = 0; i < lines.Length; i++)
            if (RxEnStart.IsMatch(lines[i])) { enStart = i; break; }

        int koEnd = enStart >= 0 ? enStart : lines.Length;

        var ko = ParseRange(lines, 0, koEnd);
        ConstSections? en = enStart >= 0 ? ParseRange(lines, enStart, lines.Length) : null;
        return (ko, en);
    }

    static ConstSections ParseRange(string[] lines, int from, int to)
    {
        // 최상위 헤더들의 위치 수집
        var heads = new List<(int Num, int Line)>();
        for (int i = from; i < to; i++)
        {
            var m = RxTopHead.Match(lines[i]);
            if (!m.Success) continue;
            // 들여쓰기 없는 것만 최상위로 간주
            if (lines[i].Length > 0 && char.IsWhiteSpace(lines[i][0])) continue;
            heads.Add((int.Parse(m.Groups[1].Value), i));
        }

        var cs = new ConstSections();
        for (int k = 0; k < heads.Count; k++)
        {
            var (num, line) = heads[k];
            int end = k + 1 < heads.Count ? heads[k + 1].Line : to;

            // "- END -" 같은 꼬리 제거
            var body = new List<string>();
            for (int j = line; j < end; j++)
            {
                var t = lines[j].TrimEnd();
                if (Regex.IsMatch(t, @"^\s*-\s*END\s*-\s*$", RegexOptions.IgnoreCase)) break;
                body.Add(lines[j].TrimEnd());
            }
            // 뒤쪽 빈 줄 제거
            while (body.Count > 0 && body[^1].Trim().Length == 0) body.RemoveAt(body.Count - 1);
            string block = string.Join("\r\n", body);

            switch (num)
            {
                case 0: cs.S0 = block; break;
                case 2: cs.S2 = block; break;
                case 3: cs.S3 = block; break;
                case 5: cs.S5 = block; break;
                case 7: cs.S7 = block; break;
                case 8: cs.S8 = block; break;
                case 9: cs.S9 = block; break;
                // 1, 4, 6 은 생성되므로 무시
            }
        }
        return cs;
    }
}
