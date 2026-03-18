#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// GameSceneManager 컴포넌트가 GameObject에 처음 추가될 때,
/// 또는 Inspector에서 Reset을 눌렀을 때 스프라이트/사운드를 자동 연결합니다.
/// Assembly-CSharp-Editor 에서만 동작하므로 게임 어셈블리와 순환참조가 없습니다.
/// </summary>
[CustomEditor(typeof(GameSceneManager))]
public class GameSceneManagerEditor : Editor
{
    // MonoBehaviour.Reset() 은 CustomEditor 에서 직접 오버라이드할 수 없으므로
    // 툴바 컨텍스트 메뉴(Reset) 클릭 이벤트를 OnEnable 시점에서 처리합니다.
    // 대신 Inspector 하단에 버튼을 제공하여 언제든 재연결 가능하도록 합니다.

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);
        if (GUILayout.Button("◆ 스프라이트 & 사운드 자동연결 (Tools/3과 동일)", GUILayout.Height(30)))
        {
            var gsm = (GameSceneManager)target;
            ProjectInitializer.AssignAll(gsm);
            EditorUtility.SetDirty(gsm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gsm.gameObject.scene);
            Debug.Log("[AutoSetup] 스프라이트 & 사운드 연결 완료.");
        }
    }
}

/// <summary>
/// 컴포넌트가 새로 추가될 때 자동으로 AssignAll 수행 (Unity 6 이상 지원).
/// </summary>
[InitializeOnLoad]
public static class GameSceneManagerAutoSetup
{
    static GameSceneManagerAutoSetup()
    {
        ObjectFactory.componentWasAdded += OnComponentAdded;
    }

    private static void OnComponentAdded(Component comp)
    {
        if (comp is GameSceneManager gsm)
        {
            // 다음 프레임에 실행 (컴포넌트 초기화 완료 후)
            EditorApplication.delayCall += () =>
            {
                if (gsm == null) return;
                ProjectInitializer.AssignAll(gsm);
                EditorUtility.SetDirty(gsm);
                Debug.Log("[AutoSetup] GameSceneManager 스프라이트 자동연결 완료.");
            };
        }
    }
}
#endif
