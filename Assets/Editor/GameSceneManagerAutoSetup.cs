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

// ====================================================================
// 각인 데이터 에셋 생성 (Tools > 7. 각인 데이터 생성)
// ====================================================================
public static class EngravingDataCreator
{
    private struct EngravingTemplate
    {
        public string name;
        public string description;
        public bool   isDebuff;
        public EngravingEffectType effectType;
        public float  value;
    }

    [MenuItem("Tools/7. 각인 데이터 생성 (10종)")]
    public static void CreateDefaultEngravings()
    {
        // Resources/Engravings 폴더 생성
        const string FOLDER = "Assets/Resources/Engravings";
        if (!System.IO.Directory.Exists(FOLDER))
            System.IO.Directory.CreateDirectory(FOLDER);
        AssetDatabase.Refresh();

        var templates = new EngravingTemplate[]
        {
            new EngravingTemplate { name="강철 의지",     description="극한의 의지로 최대 체력이 증가한다.",            isDebuff=false, effectType=EngravingEffectType.MaxHpBonus,        value=30f  },
            new EngravingTemplate { name="날카로운 감각",  description="감각이 예리해져 공격력이 증가한다.",              isDebuff=false, effectType=EngravingEffectType.AttackBonus,       value=5f   },
            new EngravingTemplate { name="가벼운 발걸음",  description="몸이 가벼워져 어그로 감지 범위가 절반이 된다.",    isDebuff=false, effectType=EngravingEffectType.AggroRangeMultiplier,value=0.5f },
            new EngravingTemplate { name="회복의 기도",    description="물약의 회복 효과가 강화된다.",                    isDebuff=false, effectType=EngravingEffectType.PotionHealBonus,   value=15f  },
            new EngravingTemplate { name="불사의 맹세",    description="극적인 순간에 부활 확률이 높아진다.",              isDebuff=false, effectType=EngravingEffectType.ReviveChanceBonus, value=0.15f},
            new EngravingTemplate { name="독의 각인",      description="몸속에 독이 퍼져 체력이 서서히 감소한다.",         isDebuff=true,  effectType=EngravingEffectType.ForceApplyPoison,  value=1f   },
            new EngravingTemplate { name="피로의 족쇄",    description="만성 피로로 최대 체력이 감소한다.",                isDebuff=true,  effectType=EngravingEffectType.MaxHpBonus,        value=-20f },
            new EngravingTemplate { name="화염의 낙인",    description="온몸에 화상을 입어 지속적인 피해를 받는다.",        isDebuff=true,  effectType=EngravingEffectType.ForceApplyBurn,    value=1f   },
            new EngravingTemplate { name="악마와의 계약",  description="막대한 힘을 얻지만 최대 체력이 감소한다.",         isDebuff=true,  effectType=EngravingEffectType.AttackBonus,       value=10f  },
            new EngravingTemplate { name="광전사의 혼",    description="분노가 공격력을 올리지만 방어력을 낮춘다.",         isDebuff=true,  effectType=EngravingEffectType.DefenseBonus,      value=-3f  },
        };

        int created = 0;
        var createdAssets = new System.Collections.Generic.List<EngravingData>();

        foreach (var t in templates)
        {
            string path = $"{FOLDER}/{t.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<EngravingData>(path) != null)
            {
                Debug.Log($"[EngravingCreator] 이미 존재: {t.name}");
                createdAssets.Add(AssetDatabase.LoadAssetAtPath<EngravingData>(path));
                continue;
            }
            var asset = ScriptableObject.CreateInstance<EngravingData>();
            asset.engravingName = t.name;
            asset.description   = t.description;
            asset.isDebuff      = t.isDebuff;
            asset.effects       = new EngravingEffect[]
            {
                new EngravingEffect { effectType = t.effectType, value = t.value }
            };
            AssetDatabase.CreateAsset(asset, path);
            createdAssets.Add(asset);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // GameManager 프리팹의 EngravingManager.engravingPool 자동 등록
        const string PREFAB_PATH = "Assets/Resources/GameManager.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab != null)
        {
            var em = prefab.GetComponentInChildren<EngravingManager>(true);
            if (em != null)
            {
                em.engravingPool = createdAssets.ToArray();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log($"[EngravingCreator] GameManager 프리팹에 {createdAssets.Count}개 등록 완료.");
            }
        }

        Debug.Log($"[EngravingCreator] 각인 에셋 {created}개 생성 완료 (총 {createdAssets.Count}개).");
        EditorUtility.DisplayDialog("각인 데이터 생성 완료",
            $"{created}개 생성, {createdAssets.Count - created}개 기존\n경로: {FOLDER}", "확인");
    }
}
#endif
