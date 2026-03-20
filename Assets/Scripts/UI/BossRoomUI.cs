// ============================================================
// BossRoomUI.cs
// 보스 룸 진입 시 화면 상단에 표시되는 보스 정보 UI.
//   - 보스 이름 · 설명 텍스트
//   - 체력 바 (Slider + "현재HP / 최대HP" 텍스트 표시)
//   - 진입 시 자동으로 보스 전용 BGM 재생
//
// 사용 방법:
//   1. Canvas 하위에 BossHUD 오브젝트를 만들고 이 컴포넌트를 부착.
//   2. Inspector에서 각 UI 참조를 연결.
//   3. 던전 로직에서 보스 룸 진입 시 Show(enemyData, currentHp, maxHp) 호출.
//   4. 보스 HP 변경 시마다 UpdateHp(currentHp, maxHp) 호출.
//   5. 보스 룸 퇴장 또는 보스 처치 시 Hide() 호출.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossRoomUI : MonoBehaviour
{
    // ── Inspector 연결 ────────────────────────────────────────────────────────
    [Header("패널 루트 (비활성↔활성 전환)")]
    [Tooltip("보스 HUD 전체를 감싸는 루트 GameObject")]
    public GameObject hudRoot;

    [Header("보스 정보 텍스트")]
    [Tooltip("보스 이름 TMP 텍스트")]
    public TMP_Text bossNameText;

    [Tooltip("보스 설명 TMP 텍스트")]
    public TMP_Text bossDescText;

    [Header("체력 바")]
    [Tooltip("체력 Slider (Min=0, Max=1 기준 정규화 값)")]
    public Slider   hpSlider;

    [Tooltip("'현재HP / 최대HP' 형식으로 표시하는 TMP 텍스트")]
    public TMP_Text hpText;

    // ── 캐시 ─────────────────────────────────────────────────────────────────
    private EnemyData _currentBoss;

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 보스 룸 진입 시 호출.
    /// 보스 이름 · 설명을 표시하고, 체력 바를 초기화하며,
    /// 전용 BGM이 있으면 재생한다.
    /// </summary>
    public void Show(EnemyData boss, int currentHp, int maxHp)
    {
        _currentBoss = boss;

        if (hudRoot != null)
            hudRoot.SetActive(true);

        if (bossNameText != null)
            bossNameText.text = boss != null ? boss.enemyName : "";

        if (bossDescText != null)
            bossDescText.text = boss != null ? boss.description : "";

        UpdateHp(currentHp, maxHp);

        // 전용 BGM 재생
        if (boss != null && boss.bossBattleBgm != null)
            GameManager.Instance?.Audio?.PlayBgm(boss.bossBattleBgm);
    }

    /// <summary>
    /// 보스 HP 변경 시마다 호출. 슬라이더와 텍스트를 갱신한다.
    /// </summary>
    public void UpdateHp(int currentHp, int maxHp)
    {
        if (hpSlider != null)
            hpSlider.value = maxHp > 0 ? (float)currentHp / maxHp : 0f;

        if (hpText != null)
            hpText.text = $"{currentHp} / {maxHp}";
    }

    /// <summary>
    /// 보스 룸 이탈 또는 보스 처치 시 호출. HUD를 숨긴다.
    /// </summary>
    public void Hide()
    {
        if (hudRoot != null)
            hudRoot.SetActive(false);

        _currentBoss = null;
    }

    /// <summary>현재 표시 중인 보스 데이터를 반환.</summary>
    public EnemyData CurrentBoss => _currentBoss;

    // ── Unity 생명주기 ────────────────────────────────────────────────────────
    void Start()
    {
        // 시작 시 HUD 숨김
        if (hudRoot != null)
            hudRoot.SetActive(false);
    }
}
