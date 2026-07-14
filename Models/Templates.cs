namespace Sibang_generator.Models;

/// <summary>
/// 상수 섹션 0,2,3,5,7,8,9 의 본문.
/// 기존 시방 txt 를 첨부하면 이 값들을 파싱해서 덮어쓴다.
/// 첨부가 없으면 이 기본값(또는 사용자가 톱니바퀴에서 편집한 값)을 쓴다.
///
/// 각 필드는 "해당 섹션의 전체 블록"(헤더 줄 포함)을 담는다.
/// </summary>
public sealed class ConstSections
{
    public string S0 { get; set; } = "";   // 0. 시방목적
    public string S2 { get; set; } = "";   // 2. 모델타입
    public string S3 { get; set; } = "";   // 3. 적용시점
    public string S5 { get; set; } = "";   // 5. 변경내용
    public string S7 { get; set; } = "";   // 7. 출하환경
    public string S8 { get; set; } = "";   // 8. 재고
    public string S9 { get; set; } = "";   // 9. 기타

    public static ConstSections DefaultKo() => new()
    {
        S0 = "0. 시방목적 : 양산 시방",
        S2 = "2. 모델타입 : C309",
        S3 = "3. 적용시점 : 시방 배포 후 즉시",
        S5 = "5. 변경내용 : -",
        S7 = "7. 출하환경\r\n    - 출하검사 Guide를 참고하여 진행",
        S8 = "8. 재고\r\n    - 없음",
        S9 = "9. 기타\r\n   - 없음",
    };

    public static ConstSections DefaultEn() => new()
    {
        S0 = "0. Purpose : MP",
        S2 = "2. Model Type : C309",
        S3 = "3. Application Time : Immediately",
        S5 = "5. Change Details : -",
        S7 = "7. Shipping environment\r\n    - Proceed by referring to the Shipping Inspection Guide",
        S8 = "8. Stock\r\n    - Doesn't exist",
        S9 = "9. Other\r\n   - Doesn't exist",
    };
}

/// <summary>
/// 6. 제조환경의 고정 골격 문구.
/// 모델 리스트가 들어가는 자리는 비워두고, 생성기가 채운다.
/// 한/영 두 벌로 관리하며, 모델 리스트만 양쪽에 동일하게 넣는다.
/// </summary>
public sealed class Section6Template
{
    // 헤더 / 소제목
    public string Header { get; set; } = "";              // "6. 제조환경"
    public string CopyIntro { get; set; } = "";           // "- Copy 공정에서 Region/Theme 적용되어야 함"
    public string CmdLine { get; set; } = "";             // "1) Copy 지그에서 제품으로 CMD 전송"
    public string BootLine { get; set; } = "";            // "2) CMD 전송 이후 Test Mode로 부팅"
    public string TestsIntro { get; set; } = "";          // "- Copy 이후 Test Mode에서 ..."
    public string TestsBody { get; set; } = "";           // 전류검사 ~ RTC 검사 (여러 줄)
    public string LcdIntro { get; set; } = "";            // "- 검사 이후에 ... User Mode 화면이 나와야 함"
    public string LcdWifiIntro { get; set; } = "";        // "1) 좌측 상단에 ... 확인"
    public string LcdMiddle { get; set; } = "";           // 2) ~ 8) 고정 줄들
    public string LcdRegionIntro { get; set; } = "";      // "9) Region, Language, Theme 확인"
    public string SwVersionLabel { get; set; } = "";      // "3) S/W Version" / "3) Check S/W Version"

    // 재작업 (6-1) 관련
    public string ReworkHeader { get; set; } = "";        // "6-1) 구미 재작업 공정"
    public string JigHeader { get; set; } = "";           // "6-2) JIG 사용 공정"

    public static Section6Template DefaultKo() => new()
    {
        Header = "6. 제조환경",
        CopyIntro = "- Copy 공정에서 Region/Theme 적용되어야 함",
        CmdLine = "1) Copy 지그에서 제품으로 CMD 전송",
        BootLine = "2) CMD 전송 이후 Test Mode로 부팅",
        TestsIntro = "- Copy 이후 Test Mode에서 하기 검사들을 JIG에서 실시 해야함",
        TestsBody =
            "1) 전류검사\r\n" +
            "2) Touch 동작 검사\r\n" +
            "3) 화면 검사\r\n" +
            "4) 온습도 검사\r\n" +
            "5) 통신 검사\r\n" +
            "6) 리모컨 IR 수신 검사\r\n" +
            "7) Wi-Fi 통신 검사\r\n" +
            "    - Wi-Fi 검사시 \"T206 BLE 공유기\" (Wi-Fi, BLE 콤보 검사하는 공유기) 가 켜져 있는 상태에서 진행 필요\r\n" +
            "8) Buzzer 검사\r\n" +
            "9) RTC 검사",
        LcdIntro = "- 검사 이후에 아래 LCD 화면에 나오는 결과를 만족한 후 1번 key 누름 -> LCD 화면에 \"Mode : Change Mode\" 및 [UserMode] 확인 -> HW Reset(S1)후 LCD화면에 User Mode 화면이 나와야 함",
        LcdWifiIntro = "1) 좌측 상단에 'Wi-Fi', 'Buzzer', 또는 '–' 중 하나가 표시되는지 확인",
        LcdMiddle =
            "2) 우측 상단에 시간 표시되는지 확인\r\n" +
            "{SW}\r\n" +
            "4) Temp/Humi : 현재 온도/습도 값 표시\r\n" +
            "5) Communication : OK 확인\r\n" +
            "6) IR Receiver : OK 확인\r\n" +
            "7) Wi-Fi : '--' 확인\r\n" +
            "8) RTC : OK 확인",
        LcdRegionIntro = "9) Region, Language, Theme 확인",
        SwVersionLabel = "3) S/W Version",
        ReworkHeader = "6-1) 구미 재작업 공정",
        JigHeader = "6-2) JIG 사용 공정",
    };

    public static Section6Template DefaultEn() => new()
    {
        Header = "6. Manufacturing environment",
        CopyIntro = "- Region/Theme must be applied in the Copy process",
        CmdLine = "1) CMD transmission from Copy jig to product",
        BootLine = "2) Boot into Test Mode after sending CMD",
        TestsIntro = "- After copying, the following tests must be performed on the JIG in Test Mode.",
        TestsBody =
            "1) Current test\r\n" +
            "2) Touch operation test\r\n" +
            "3) Screen test\r\n" +
            "4) Temperature and humidity test\r\n" +
            "5) Communication test\r\n" +
            "6) Remote control IR reception test\r\n" +
            "7) Wi-Fi communication test\r\n" +
            "    - When checking Wi-Fi, it is necessary to proceed with the \"T206 BLE router\" (Router testing Wi-Fi, BLE combo) turned on.\r\n" +
            "8) Buzzer test\r\n" +
            "9) RTC test",
        LcdIntro = "- After the test, if you are satisfied with the result displayed on the LCD screen below, press key 1 -> Check \"Mode: Change Mode\" and [UserMode] on the LCD screen -> After HW Reset (S1), the User Mode screen should appear on the LCD screen",
        LcdWifiIntro = "1) Verify that one of the following is displayed in the upper-left corner: \u201cWi-Fi\u201d, \u201cBuzzer\u201d, or \u201c\u2013\u201d",
        LcdMiddle =
            "2) Check if the time is displayed on the upper right\r\n" +
            "{SW}\r\n" +
            "4) Temp/Humi: Displays the current temperature/humidity value\r\n" +
            "5) Check Communication : OK\r\n" +
            "6) Check IR Receiver : OK\r\n" +
            "7) Check Wi-Fi : OK\r\n" +
            "8) Check RTC : OK",
        LcdRegionIntro = "9) Check Region, Language, Theme",
        SwVersionLabel = "3) Check S/W Version",
        ReworkHeader = "6-1) Gumi Rework Process",
        JigHeader = "6-2) JIG-Aided Process",
    };
}
