#if UNITY_EDITOR
// ============================================================
// DialogueSampleCreator.cs
// 硫붾돱: Game/Dialogue/Create Sample Dialogues
//
// 湲곗〈 ?뚯씪???쇨큵 ??젣?섍퀬 ?щ컮瑜??ㅽ겕由쏀듃 李몄“濡??ъ깮?깊빀?덈떎.
// DialogueSet.cs / DialogueDatabase.cs 媛 媛곸옄 蹂꾨룄 ?뚯씪???덉뼱??
// Unity 媛 m_Script 瑜??щ컮瑜닿쾶 ?뺤젙?????덉뒿?덈떎.
// ============================================================
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class DialogueSampleCreator
{
    private const string OUT_DIR = "Assets/Resources/Dialogues";
    private const string DB_PATH = "Assets/Resources/DialogueDatabase.asset";

    [MenuItem("Game/Dialogue/Create Sample Dialogues")]
    public static void CreateSamples()
    {
        // ?? 湲곗〈 ?뚯넀 asset ?꾨? ??젣 ????????????????????????????????????????
        if (AssetDatabase.DeleteAsset(DB_PATH))
            Debug.Log("[DialogueSampleCreator] 湲곗〈 DialogueDatabase.asset ??젣.");

        if (AssetDatabase.IsValidFolder(OUT_DIR))
        {
            var existing = AssetDatabase.FindAssets("t:DialogueSet", new[] { OUT_DIR });
            foreach (var guid in existing)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            // ?대뜑 ?먯껜????젣 ???ъ깮?깊븯??源붾걫?섍쾶
            AssetDatabase.DeleteAsset(OUT_DIR);
        }

        AssetDatabase.CreateFolder("Assets/Resources", "Dialogues");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var setsToRegister = new List<DialogueSet>();

        // ================================================================
        // 議곌굔 ?????2媛?
        // ================================================================

        setsToRegister.Add(CreateSet("Cond_Floor10",
            DialogueConditionType.Ach_Floor10,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?? 10痢듦퉴吏 ?꾨떖?덇뎔?? ??⑦빐?? 紐⑦뿕媛 ?묐컲. " +
                    "蹂댄넻 ?щ엺?ㅼ? 3痢듭뿉??堉덈쭔 ?④쾶 ?섏???"),
                Line(CharacterType.Player, "",
                    "?댁씠 醫뗭븯??肉먯씠?먯슂. ?꾩쭅 源딆? 痢듭뿏 萸붽? 臾댁꽌??寃껊뱾???덈뒗 ?먮굦?닿퀬??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "寃몄넀?섍뎔?? ?댁쮯???ㅻ뒛 諛ㅼ? ??理쒓퀬???좎쓣 ?대뱶由닿쾶?? " +
                    "嫄닿컯, ?꾨땲 ?앹〈???꾪븯??"),
            }
        ));

        setsToRegister.Add(CreateSet("Cond_Gold1000",
            DialogueConditionType.Ach_Gold1000,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?대㉧?? 二쇰㉧?덇? ?쒕쾿 ?먮몣?댁??④뎔?? ?섏쟾?먯꽌 ?⑷툑?대씪??湲곸뼱?ㅼ뀲?섏슂?"),
                Line(CharacterType.Player, "",
                    "?붿쬁 蹂대Ъ ?곸옄 ?댁씠 醫뗫뜑?쇨퀬?? 紐ъ뒪?곕뱾???띿뿉 湲덊솕瑜??붾쑊 吏?덇퀬 ?ㅻ땲?붽뎔??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?섑븯, ?섏쟾 紐ъ뒪?곌? 臾댁뒯 ?섏쟾?곷룄 ?꾨땶??留먯씠二? " +
                    "萸먭? ?먮뱺 ???곗꽭?? ??쒗뀒 ?곗뀛??醫뗪퀬?? ?먰쓲."),
            }
        ));

        // ================================================================
        // ?쇰컲 ?????10媛?
        // ================================================================

        setsToRegister.Add(CreateSet("Gen_Morning",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?댁꽌 ?ㅼ꽭?? ?ㅻ뒛???섏쟾???꾩쟾?섏떎 嫄닿??? " +
                    "?꾩묠 ?앹궗???섏뀲?섏슂? 鍮덉냽?쇰줈 ?섏쟾 ?ㅼ뼱媛硫??곗씪 ?섏???"),
                Line(CharacterType.Player, "",
                    "??긽 ?좉꼍 ?⑥＜?붿꽌 媛먯궗?댁슂. ?ㅻ뒛????遺?곷뱶由쎈땲??"),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Weather",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?ㅻ뒛? 諛뽰씠 ?좊룆 ?먮━?ㅼ슂. 洹몃옒???섏쟾 ?덉? ??긽 ?대몼怨?異뺤텞?섎땲 " +
                    "?좎뵪 ?곴??놁씠 ?뚯궛?섍릿 ?섏???"),
                Line(CharacterType.Player, "",
                    "洹몃윭寃뚯슂. ?섏쟾?먯꽑 留묒? ?섎뒛??蹂?湲곗뼲???녿꽕??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "洹몃옒???댁븘???뚯븘?ㅻ㈃ 瑗??섎뒛???쒕쾲 ?щ젮?ㅻ낫?몄슂. " +
                    "洹멸쾶 ?쇰쭏???뚯쨷?쒖? ?덉궪 ?먮겮寃??쒕떟?덈떎."),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Rumor",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?붿쬁 ?곸씤???ъ씠???뚮Ц???뚮뜑援곗슂. ?섏쟾 源딆닕??怨녹뿉 " +
                    "?좊뱺 ?⑹씠 ?덈떎??嫄곗삁?? 萸? ?뚮Ц?닿쿋吏留?.."),
                Line(CharacterType.Player, "",
                    "?⑹씠?? ?꾩쭅 洹몃윴 嫄?紐?遊ㅻ뒗?? ??源딆씠 ?대젮媛硫??뚭쾶 ??寃?媛숈??곗슂."),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?쒕컻 留뚮굹吏 ?딅뒗 寃?理쒖꽑?낅땲?? 紐⑦뿕媛 ?묐컲."),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Equipment",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?좉퉸留뚯슂, 諛⑺뙣??湲덉씠 媛?嫄?蹂댁씠?쒕굹?? " +
                    "?섏쟾 ?ㅼ뼱媛湲??꾩뿉 ?λ퉬 ?곹깭瑜??쒕쾲 ???뺤씤?섏꽭??"),
                Line(CharacterType.Player, "",
                    "?? ?뺣쭚?대꽕?? ?댁젣 怨⑤젞?쒗뀒 醫 ?멸쾶 留욎븯嫄곕뱺?? ?섎━?댁빞寃좉뎔??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "留덉쓣????μ옣???⑤━?⑦븳??媛蹂댁꽭?? ?쒖뵪媛 苑?醫뗭? 遺꾩씠?먯슂."),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Tired",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.Player, "",
                    "?ㅻ뒛? 醫 ?쇨낀?섎꽕?? ?댁젣 5痢듦퉴吏 媛붾떎媛 寃⑥슦 ?덉텧?덇굅?좎슂."),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?대윴, 留롮씠 怨좎깮?섏뀲援곗슂. ?ㅻ뒛? 2痢?移⑤? 媛앹떎???밸퀎 ?좎씤???쒕┫寃뚯슂. " +
                    "???ш퀬 ?댁씪 ?ㅼ떆 ?꾩쟾?섏떆??寃??대뼥源뚯슂?"),
                Line(CharacterType.Player, "",
                    "媛먯궗?댁슂. 洹??쒖븞 諛쏆븘?ㅼ씠寃좎뒿?덈떎."),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_OtherAdventurer",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?쇰쭏 ?꾩뿉 寃? 留앺넗瑜??먮Ⅸ 紐⑦뿕媛媛 ?ㅻ?媛붿뿀?붾뜲, 洹?遺꾨룄 " +
                    "?쒕쾿 源딆씠 ?대젮媛?⑤뜑?쇨퀬?? 吏湲덉? ?대뵒 怨꾩떊吏..."),
                Line(CharacterType.Player, "",
                    "?뱀떆 ?④퍡 ?뚰떚瑜?吏쒕낵 ???덉쭊 ?딆쓣源뚯슂?"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "湲?꾩슂, 洹몃텇? ?쇱옄 ?ㅻ땲??嫄??좏샇?섏뀲?댁슂. ?좊뱺???숇즺??" +
                    "洹??대뼡 留덈쾿 ?꾩씠?쒕낫??洹以묓븳??留먯씠二?"),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Food",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?ㅻ뒛 ?뱀젣 ?섏쟾 ?ㅽ뒠瑜?留뚮뱾?덉뼱?? " +
                    "?щ즺??臾살? 留덉꽭??.. 鍮꾨? ?덉떆?쇨굅?좎슂. ?섏?留?癒뱀쑝硫??섏씠 ?섏슂!"),
                Line(CharacterType.Player, "",
                    "?щ즺媛 醫 嫄깆젙?섏?留? 留쏆엳??蹂댁씠???꾩깉媛 ?섎뒗 嫄??ъ떎?댁뿉??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?명샇, 誘우뼱蹂댁꽭?? ?쒓? 20?꾩㎏ ???ш????댁쁺???щ엺?댁뿉??"),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_History",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "???섏쟾???앷릿 寃?踰뚯뜥 300?꾨룄 ?섏뿀?ㅻ뒗 嫄??꾩꽭?? " +
                    "?먮옒??怨좊? ?뺢뎅??吏??李쎄퀬??ㅺ퀬 ?섎뜑?쇨퀬??"),
                Line(CharacterType.Player, "",
                    "洹몃젃援곗슂. 洹몃옒??媛???ㅻ옒???좊Ъ?ㅼ씠 ?섏삤??嫄곌뎔??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "留욎븘?? 洹??좊Ъ?ㅼ씠 紐⑦뿕媛?ㅼ쓣 遺덈윭 紐⑥쑝怨? " +
                    "洹?紐⑦뿕媛???뺣텇??????ㅻ뒛???ш????댁쁺?????덈떟?덈떎, ?섑븯."),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Monster",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "理쒓렐???뚯븘??紐⑦뿕媛??留먯뿉 ?섑븯硫? 7痢?洹쇱쿂?먯꽌 " +
                    "?낆쓣 肉쒕뒗 嫄곕? 吏?ㅻ? 遊ㅻ떎怨??댁슂. 議곗떖?섏꽭??"),
                Line(CharacterType.Player, "",
                    "?낆꽦 紐ъ뒪?곕뒗 ?깃??쒖짛. ?대룆?쒕? 異⑸텇??梨숆꺼媛?쇨쿋援곗슂."),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "??留ㅼ젏???대룆???덉뼱?? 臾쇰줎... ?좊즺吏留뚯슂, ?명샇."),
            }
        ));

        setsToRegister.Add(CreateSet("Gen_Return",
            DialogueConditionType.General,
            new[]
            {
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?뚯븘?ㅼ뀲援곗슂! 嫄깆젙?덉뼱?? ?댁븘????臾몄쓣 ?ㅼ떆 ?댁뼱二쇰뒗 寃껊쭔?쇰줈??" +
                    "?뺣쭚 ?ㅽ뻾?댁뿉??"),
                Line(CharacterType.Player, "",
                    "寃⑥슦寃⑥슦 踰꾪끉?댁슂. ???ш???諛섍컩?ㅻ뒗 寃??대젃寃??ㅺ컧?섎뒗 ?좎씠 ?녿꽕??"),
                Line(CharacterType.NPC01_Innkeeper, "",
                    "?? ?④굅??李??????쒖꽭?? 洹몃━怨??ㅻ뒛 ?덉뿀???쇱쓣 ?ㅻ젮二쇱꽭?? " +
                    "???紐⑦뿕媛 遺꾨뱾???댁빞湲곕? ?ｋ뒗 寃??쒖씪 利먭굅?뚯슂."),
            }
        ));

        // ================================================================
        // DialogueDatabase ?좉퇋 ?앹꽦 諛??명듃 ?깅줉
        // ================================================================
        var db            = ScriptableObject.CreateInstance<DialogueDatabase>();
        db.dialogueSets   = setsToRegister;
        AssetDatabase.CreateAsset(db, DB_PATH);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ?ㅽ겕由쏀듃 李몄“媛 ?щ컮瑜몄? 寃利?
        int broken = 0;
        foreach (var set in setsToRegister)
        {
            if (set == null) { broken++; continue; }
            var ms = UnityEditor.MonoScript.FromScriptableObject(set);
            if (ms == null) broken++;
        }

        string result = broken == 0
            ? $"?깃났! 紐⑤뱺 {setsToRegister.Count}媛??명듃媛 ?뺤긽 ?깅줉?섏뿀?듬땲??"
            : $"寃쎄퀬: {broken}媛??명듃???ㅽ겕由쏀듃 李몄“ 臾몄젣媛 ?덉뒿?덈떎.\n" +
              "Unity 媛 而댄뙆?쇱쓣 ?꾨즺?????ㅼ떆 ?ㅽ뻾?섏꽭??";

        Debug.Log($"[DialogueSampleCreator] {result}");
        EditorUtility.DisplayDialog("????섑뵆 ?앹꽦", result, "?뺤씤");
    }

    private static DialogueSet CreateSet(string assetName,
        DialogueConditionType condition, DialogueLine[] lines)
    {
        var set       = ScriptableObject.CreateInstance<DialogueSet>();
        set.condition = condition;
        set.lines     = new List<DialogueLine>(lines);
        AssetDatabase.CreateAsset(set, $"{OUT_DIR}/{assetName}.asset");
        return set;
    }

    private static DialogueLine Line(CharacterType ch, string sprite, string text)
        => new DialogueLine { character = ch, text = text };  // faceSprite는 Inspector에서 직접 지정
}
#endif
