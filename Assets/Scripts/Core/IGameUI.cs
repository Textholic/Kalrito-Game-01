// ============================================================
// IGameUI.cs
// GameManager가 UI에 접근하기 위한 인터페이스.
// GameScene의 HUD 컨트롤러 등이 구현.
// ============================================================
public interface IGameUI
{
    void AddLog(string message);
    void RefreshHUD();
}
