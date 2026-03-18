using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 시작 시 항상 TitleScene에서 실행되도록 보장합니다.
/// Unity 에디터에서 GameScene이 열린 채로 Play를 눌러도 TitleScene부터 시작합니다.
/// GameManager 싱글턴이 없으면 Resources/GameManager 프리팹에서 자동 스폰합니다.
/// </summary>
public class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // GameManager 싱글턴 보장
        if (GameManager.Instance == null)
        {
            var prefab = Resources.Load<GameObject>("GameManager");
            if (prefab != null)
                Object.Instantiate(prefab);
            else
            {
                // 프리팹이 없으면 런타임에 직접 생성
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
        }

        // 타이틀 씬 강제 시작
        if (SceneManager.GetActiveScene().name != "TitleScene")
            SceneManager.LoadScene("TitleScene");
    }
}
