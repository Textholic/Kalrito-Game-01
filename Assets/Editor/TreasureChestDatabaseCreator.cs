#if UNITY_EDITOR
// ============================================================
// TreasureChestDatabaseCreator.cs
// Tools 메뉴에서 TreasureChestDatabase.asset 을 생성하고
// 소비형 30개 + 장비 30개를 자동으로 채워 넣는 에디터 유틸.
// 스프라이트는 Brackeys 에셋에서 자동 할당하며,
// 이후 Inspector에서 교체 가능합니다.
// ============================================================
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class TreasureChestDatabaseCreator
{
    private const string ASSET_PATH = "Assets/Resources/TreasureChestDatabase.asset";
    private const string BASE       = "Assets/Brackeys/2D Mega Pack/Items & Icons/";
    private const string PIX        = BASE + "Pixel Art/";
    private const string GOTH       = BASE + "Gothic/";

    [MenuItem("Tools/6. Create Treasure Chest Database")]
    public static void CreateDatabase()
    {
        // Resources 폴더 보장
        if (!Directory.Exists(Application.dataPath + "/Resources"))
        {
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            AssetDatabase.Refresh();
        }

        TreasureChestDatabase db = AssetDatabase.LoadAssetAtPath<TreasureChestDatabase>(ASSET_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<TreasureChestDatabase>();
            AssetDatabase.CreateAsset(db, ASSET_PATH);
        }

        db.consumables.Clear();
        db.equipments.Clear();

        // ── 스프라이트 로드 헬퍼 ─────────────────────────────────────────────
        Sprite Spr(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[ChestDB] 스프라이트 없음: {path}");
            return s;
        }

        // ── 소비형 아이템 30개 ────────────────────────────────────────────────
        var consumables = new List<ConsumableItemDef>
        {
            // 상태이상 해제 물약 (6종)
            new ConsumableItemDef { id="cure_burn",      displayName="화상 해제 물약",    description="화상 상태이상을 즉시 해제합니다.",   icon=Spr(PIX+"Potion.png"),   cureEffect=StatusEffectType.Burn,      minFloor=1,  flavorText="불꽃도 이 앞에선 잦아든다."  },
            new ConsumableItemDef { id="cure_poison",    displayName="해독 물약",         description="중독 상태이상을 즉시 해제합니다.",   icon=Spr(PIX+"Potion.png"),   cureEffect=StatusEffectType.Poison,    minFloor=1,  flavorText="죽음의 맛을 씻어내는 쓴 약."  },
            new ConsumableItemDef { id="cure_fatigue",   displayName="활력 물약",         description="피로 상태이상을 즉시 해제합니다.",   icon=Spr(PIX+"Potion.png"),   cureEffect=StatusEffectType.Fatigue,   minFloor=2,  flavorText="지친 몸에 새 바람이 분다."  },
            new ConsumableItemDef { id="cure_charm",     displayName="정신 각성 물약",    description="매료 상태이상을 즉시 해제합니다.",   icon=Spr(PIX+"Potion.png"),   cureEffect=StatusEffectType.Charm,     minFloor=3,  flavorText="마음의 안개가 걷힌다."  },
            new ConsumableItemDef { id="cure_confusion", displayName="혼란 해제 물약",    description="혼란 상태이상을 즉시 해제합니다.",   icon=Spr(PIX+"Potion.png"),   cureEffect=StatusEffectType.Confusion, minFloor=3,  flavorText="흐트러진 생각이 가지런히 정돈된다."  },
            new ConsumableItemDef { id="cure_all",       displayName="만능 해독제",       description="모든 상태이상을 즉시 해제합니다.\n희귀 조합물약.",  icon=Spr(GOTH+"SoulFragment.png"), cureEffect=StatusEffectType.None, minFloor=5,  flavorText="어떤 저주도 이 앞에선 무색해진다."  },

            // 골드 획득 아이템 (5종)
            new ConsumableItemDef { id="gold_sm",    displayName="동전 주머니",    description="용돈이 생겼다. +30G",           icon=Spr(PIX+"Coin.png"),    goldGain=30,   minFloor=1,  flavorText="누군가 서둘러 떨어뜨린 듯하다."  },
            new ConsumableItemDef { id="gold_md",    displayName="은화 주머니",    description="은화가 가득하다. +80G",         icon=Spr(PIX+"Coin.png"),    goldGain=80,   minFloor=2,  flavorText="달빛 아래 은빛으로 빛난다."  },
            new ConsumableItemDef { id="gold_lg",    displayName="금화 주머니",    description="묵직한 금화. +150G",           icon=Spr(PIX+"Diamond.png"), goldGain=150,  minFloor=4,  flavorText="황금의 무게가 손에 전해진다."  },
            new ConsumableItemDef { id="gold_gem",   displayName="보석 원석",      description="아름다운 원석. +300G",         icon=Spr(PIX+"Diamond.png"), goldGain=300,  minFloor=6,  flavorText="대지의 심장에서 탄생한 광채."  },
            new ConsumableItemDef { id="gold_crown", displayName="왕관 파편",      description="부서진 왕관의 일부. +500G",    icon=Spr(PIX+"Crown.png"),   goldGain=500,  minFloor=8,  flavorText="왕좌는 무너졌어도 금은 남는다."  },

            // 레벨+1
            new ConsumableItemDef { id="levelup_tome",   displayName="경험의 서",     description="읽는 즉시 경험치가 넘쳐 레벨이 오릅니다.",   icon=Spr(PIX+"Scroll.png"), levelUp=true, minFloor=3,  flavorText="수많은 전사의 지혜가 한 장에 담겼다."  },
            new ConsumableItemDef { id="levelup_elixir", displayName="성장의 영약",   description="몸 전체에 성장의 기운이 퍼진다. Lv+1",       icon=Spr(GOTH+"SoulFragment.png"), levelUp=true, minFloor=6, flavorText="마시는 순간 몸이 다시 태어나는 느낌."  },

            // HP 회복
            new ConsumableItemDef { id="hp_sm",   displayName="회복 물약 (소)",  description="HP를 25 회복합니다.",   icon=Spr(PIX+"Potion.png"),  hpHeal=25,  minFloor=1,  flavorText="상처가 조금 아물었다."  },
            new ConsumableItemDef { id="hp_md",   displayName="회복 물약 (중)",  description="HP를 60 회복합니다.",   icon=Spr(PIX+"Potion.png"),  hpHeal=60,  minFloor=2,  flavorText="붉은 액체가 생기를 불어넣는다."  },
            new ConsumableItemDef { id="hp_lg",   displayName="회복 물약 (대)",  description="HP를 120 회복합니다.", icon=Spr(GOTH+"SoulFragment.png"), hpHeal=120, minFloor=4, flavorText="상처는 기억도 못할 듯 사라진다."  },
            new ConsumableItemDef { id="hp_full", displayName="생명의 샘물",     description="HP를 완전히 회복합니다.", icon=Spr(PIX+"WaterDrop1.png"), hpHeal=9999, minFloor=7, flavorText="전설 속 불사의 샘, 그 한 모금."  },

            // 최대 HP 증가
            new ConsumableItemDef { id="maxhp_sm",   displayName="강인함의 약초",  description="최대 HP +10",   icon=Spr(PIX+"Meat.png"),  maxHpBonus=10,  minFloor=1,  flavorText="뿌리 깊은 나무처럼, 몸이 단단해진다."  },
            new ConsumableItemDef { id="maxhp_md",   displayName="생명력 결정",    description="최대 HP +25",   icon=Spr(PIX+"Earth.png"), maxHpBonus=25,  minFloor=3,  flavorText="생명의 불꽃이 더 오래 타오른다."  },
            new ConsumableItemDef { id="maxhp_lg",   displayName="불사의 심장",    description="최대 HP +50",   icon=Spr(PIX+"Heart.png"), maxHpBonus=50,  minFloor=6,  flavorText="죽음조차 비껴가는 힘이 솟는다."  },

            // 공격력 증가
            new ConsumableItemDef { id="atk_sm",   displayName="전사의 분노 물약",   description="공격력 +5",    icon=Spr(PIX+"Fire.png"),   attackBonus=5,  minFloor=1,  flavorText="분노가 힘이 될 때도 있다."  },
            new ConsumableItemDef { id="atk_md",   displayName="강인함의 영약",      description="공격력 +12",   icon=Spr(PIX+"Coal.png"),   attackBonus=12, minFloor=4,  flavorText="억눌린 힘이 해방되는 감각."  },
            new ConsumableItemDef { id="atk_lg",   displayName="광전사의 피",        description="공격력 +25",   icon=Spr(PIX+"Rock.png"),   attackBonus=25, minFloor=7,  flavorText="피가 끓어오르고 눈이 붉게 물든다."  },

            // 방어력 증가
            new ConsumableItemDef { id="def_sm",   displayName="돌껍질 물약",    description="방어력 +5",    icon=Spr(PIX+"Sand.png"),  defenseBonus=5,  minFloor=2,  flavorText="피부가 돌처럼 굳어지는 느낌."  },
            new ConsumableItemDef { id="def_md",   displayName="철갑 영약",      description="방어력 +12",   icon=Spr(PIX+"Iron.png"),  defenseBonus=12, minFloor=5,  flavorText="쇠처럼 단단한 의지가 몸을 감싼다."  },
            new ConsumableItemDef { id="def_lg",   displayName="수호의 방패 물약", description="방어력 +25", icon=Spr(PIX+"Glass.png"), defenseBonus=25, minFloor=8,  flavorText="어떤 검도 뚫지 못하는 벽이 된다."  },

            // 복합 버프
            new ConsumableItemDef { id="combo_1",  displayName="전사의 비약",    description="공격력 +8, 방어력 +5",        icon=Spr(PIX+"MagicEssence.png"), attackBonus=8, defenseBonus=5, minFloor=3,  flavorText="검과 방패, 균형이 곧 생존이다."  },
            new ConsumableItemDef { id="combo_2",  displayName="용사의 혼",      description="공격력 +15, 최대 HP +30",     icon=Spr(PIX+"MagicEssence.png"), attackBonus=15, maxHpBonus=30, minFloor=5,  flavorText="용사는 강하고 질기다."  },
            new ConsumableItemDef { id="combo_3",  displayName="왕의 의지",      description="모든 스탯 +5",               icon=Spr(GOTH+"SoulFragment.png"), attackBonus=5, defenseBonus=5, maxHpBonus=20, minFloor=8, flavorText="왕위는 빼앗겨도 의지는 남는다."  },

            // 희귀·특수
            new ConsumableItemDef { id="magic_egg",   displayName="마법 알",      description="신비한 에너지가 느껴진다. HP+30, 공격+5",  icon=Spr(PIX+"Magic Egg.png"),   hpHeal=30, attackBonus=5, minFloor=2,  flavorText="무엇이 깨어날지는 열기 전엔 모른다."  },
            new ConsumableItemDef { id="world_ender", displayName="세계의 끝 파편", description="자연의 법칙을 거스른다. 레벨+1, 최대HP+20", icon=Spr(PIX+"World Ender.png"),  levelUp=true, maxHpBonus=20, minFloor=9, flavorText="세계가 끝난 자리에서 새로운 힘이 싹튼다."  },
        };

        // ── 장비 아이템 30개 ──────────────────────────────────────────────────
        var equipments = new List<EquipmentItemDef>
        {
            // 순수 공격 장비 (버프)
            new EquipmentItemDef { id="eq_sword_iron",    displayName="강철 검",       description="기본적인 강철 검. 공격력 +4",         icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Sword.png"), attackMod=4,   minFloor=1,  flavorText="단순함 속에 신뢰가 있다."  },
            new EquipmentItemDef { id="eq_axe_war",       displayName="전쟁 도끼",     description="육중한 전쟁 도끼. 공격력 +8",          icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Axe.png"),   attackMod=8,   minFloor=3,  flavorText="내리치는 순간, 전쟁이 시작된다."  },
            new EquipmentItemDef { id="eq_spear_sharp",   displayName="날카로운 창",   description="예리한 창끝. 공격력 +6.",              icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Spear.png"), attackMod=6,   minFloor=2,  flavorText="먼저 찌르는 자가 살아남는다."  },
            new EquipmentItemDef { id="eq_staff_magic",   displayName="마법 지팡이",   description="마력이 깃든 지팡이. 공격력 +10",       icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Staff.png"), attackMod=10,  minFloor=5,  flavorText="마력은 의지를 따른다."  },
            new EquipmentItemDef { id="eq_sword_flame",   displayName="화염 검",       description="불꽃이 타오르는 검. 공격력 +9",        icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Sword.png"), attackMod=9,   minFloor=4,  flavorText="검날에 깃든 불꽃은 꺼지지 않는다."  },

            // 순수 방어 장비 (버프)
            new EquipmentItemDef { id="eq_shield_wood",   displayName="나무 방패",      description="가벼운 나무 방패. 방어 +3",          icon=Spr(PIX+"Wood.png"),  defenseMod=3,   minFloor=1,  flavorText="부러져도 지키겠다는 마음은 남는다."  },
            new EquipmentItemDef { id="eq_shield_iron",   displayName="철 방패",        description="단단한 철 방패. 방어 +7",            icon=Spr(PIX+"Iron.png"),  defenseMod=7,   minFloor=3,  flavorText="불 속에서 벼린 쇠, 쉽게 굽히지 않는다."  },
            new EquipmentItemDef { id="eq_armor_leather",  displayName="가죽 갑옷",     description="기동성이 좋은 가죽 갑옷. 방어 +5",  icon=Spr(PIX+"Sheep.png"), defenseMod=5,   minFloor=2,  flavorText="빠른 발이 최고의 방어다."  },
            new EquipmentItemDef { id="eq_armor_plate",   displayName="판금 갑옷",      description="완전한 방어. 방어 +11",              icon=Spr(PIX+"Rock.png"),  defenseMod=11,  minFloor=6,  flavorText="무거울수록 더 안전하다."  },
            new EquipmentItemDef { id="eq_ring_guard",    displayName="수호의 반지",    description="방어막이 깃든 반지. 방어 +4",        icon=Spr(PIX+"TheRing.png"), defenseMod=4,  minFloor=2,  flavorText="작은 반지 하나가 생사를 가른다."  },

            // HP 장비 (버프)
            new EquipmentItemDef { id="eq_amulet_heart",  displayName="심장 목걸이",   description="생명력이 넘친다. 최대HP +40",         icon=Spr(PIX+"Heart.png"),  maxHpMod=40,  minFloor=2,  flavorText="심장이 강한 자가 오래 산다."  },
            new EquipmentItemDef { id="eq_helm_vitality", displayName="활력의 투구",   description="생명력 충전 투구. 최대HP +60",        icon=Spr(PIX+"Hat.png"),    maxHpMod=60,  minFloor=4,  flavorText="투구 속에서 의지가 불타오른다."  },
            new EquipmentItemDef { id="eq_boots_oak",     displayName="참나무 장화",   description="든든한 발. 최대HP +30",              icon=Spr(PIX+"Wood.png"),   maxHpMod=30,  minFloor=1,  flavorText="대지를 밟는 발이 강할수록 몸도 강해진다."  },

            // 복합 버프 장비
            new EquipmentItemDef { id="eq_sword_knight",  displayName="기사의 검",      description="전투 숙련자의 검. 공격 +5, 방어 +3",      icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Sword.png"), attackMod=5,  defenseMod=3,  minFloor=3,  flavorText="기사의 명예가 검날에 새겨져 있다."  },
            new EquipmentItemDef { id="eq_armor_hero",    displayName="영웅의 갑옷",    description="전설적인 갑옷. 방어 +8, 최대HP +30",        icon=Spr(PIX+"Castle.png"), defenseMod=8,  maxHpMod=30,  minFloor=7,  flavorText="영웅은 갑옷을 입기 전부터 영웅이었다."  },
            new EquipmentItemDef { id="eq_ring_power",    displayName="힘의 반지",      description="원초적인 힘. 공격 +6, 방어 +2",            icon=Spr(PIX+"TheRing.png"), attackMod=6,  defenseMod=2, minFloor=4,  flavorText="힘은 손가락 끝에서도 나온다."  },
            new EquipmentItemDef { id="eq_crown_king",    displayName="왕의 왕관",      description="절대 권력의 왕관. 공격 +10, 최대HP +50",    icon=Spr(PIX+"Crown.png"),   attackMod=10, maxHpMod=50,  minFloor=9,  flavorText="왕관을 쓰는 자, 그 무게를 견뎌야 한다."  },
            new EquipmentItemDef { id="eq_staff_heal",    displayName="치유 지팡이",    description="상처를 낫게 한다. 회복량 +15, 최대HP +20",  icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Staff.png"), healMod=15, maxHpMod=20, minFloor=3,  flavorText="치유는 공격보다 강할 때가 있다."  },

            // 디버프 장비 (높은 버프 + 패널티)
            new EquipmentItemDef { id="eq_axe_cursed",    displayName="저주받은 도끼",  description="엄청난 힘, 하지만 방어력이 낮아진다. 공격 +13, 방어 -5",  icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Axe.png"),  attackMod=13, defenseMod=-5,  minFloor=5,  flavorText="저주받은 힘은 달콤하지만 위험하다."  },
            new EquipmentItemDef { id="eq_armor_heavy",   displayName="중장 갑옷",      description="극한의 방어, 둔해진다. 방어 +15, 공격 -4",                  icon=Spr(PIX+"Iron.png"),   defenseMod=15, attackMod=-4,  minFloor=6,  flavorText="움직임을 포기하고 생존을 얻었다."  },
            new EquipmentItemDef { id="eq_ring_doom",     displayName="파멸의 반지",    description="강력한 힘의 대가. 공격 +15, 최대HP -20",                     icon=Spr(PIX+"TheRing.png"), attackMod=15, maxHpMod=-20, minFloor=7,  flavorText="파멸의 힘, 그 대가는 고통이다."  },
            new EquipmentItemDef { id="eq_staff_dark",    displayName="어둠의 지팡이",  description="어둠의 힘. 공격 +11, 방어 -3",                               icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Staff.png"), attackMod=11, defenseMod=-3, minFloor=6,  flavorText="어둠을 다루는 자는 어둠에 물든다."  },
            new EquipmentItemDef { id="eq_helm_darkness", displayName="어둠의 투구",    description="시야가 좁아진다. 방어 +9, 공격 -3",                          icon=Spr(GOTH+"Skull.png"),   defenseMod=9, attackMod=-3,  minFloor=5,  flavorText="보이지 않아도 느낄 수 있다."  },
            new EquipmentItemDef { id="eq_suit_glass",    displayName="유리 갑옷",      description="아름답지만 약하다. 공격 +8, 방어 -8, 최대HP +40",             icon=Spr(PIX+"Glass.png"),    attackMod=8,  defenseMod=-8, maxHpMod=40, minFloor=6,  flavorText="아름다운 것은 언제나 취약하다."  },

            // 고레벨 장비
            new EquipmentItemDef { id="eq_sword_dragon",  displayName="용살자의 검",    description="용을 쓰러뜨린 전설의 검. 공격 +18, 방어 +3",       icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Sword.png"), attackMod=18, defenseMod=3,  minFloor=10, flavorText="용은 죽었지만 그 혼은 검에 남았다."  },
            new EquipmentItemDef { id="eq_armor_divine",  displayName="신성한 갑옷",    description="신의 가호. 방어 +18, 최대HP +60",                   icon=Spr(PIX+"Castle.png"),   defenseMod=18, maxHpMod=60,   minFloor=10, flavorText="신의 가호 아래 아무것도 두렵지 않다."  },
            new EquipmentItemDef { id="eq_crown_divine",  displayName="신의 왕관",      description="최고의 힘. 공격+15, 방어+10, 최대HP+50",           icon=Spr(PIX+"Crown.png"),    attackMod=15, defenseMod=10, maxHpMod=50, minFloor=12, flavorText="신의 의지가 왕관에 깃들었다."  },
            new EquipmentItemDef { id="eq_soul_fragment", displayName="영혼 결정체",    description="영혼의 힘을 담았다. 공격+10, 회복+20",             icon=Spr(GOTH+"SoulFragment.png"), attackMod=10, healMod=20, minFloor=8,  flavorText="영혼의 파편이지만 그 힘은 완전하다."  },
            new EquipmentItemDef { id="eq_pickaxe_rune",  displayName="룬 곡괭이",      description="룬이 새겨진 도구. 공격 +5, 방어 +5, 최대HP +20",   icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Pickaxe.png"), attackMod=5, defenseMod=5, maxHpMod=20, minFloor=5, flavorText="룬은 시간을 초월한 지식이다."  },
            new EquipmentItemDef { id="eq_club_ancient",  displayName="고대 철퇴",      description="고대의 망치. 공격 +9, 방어 +2",                    icon=Spr("Assets/Brackeys/2D Mega Pack/Weapons & Tools/Club.png"),   attackMod=9,  defenseMod=2, minFloor=4,  flavorText="고대의 지혜가 이 무기에 담겨있다."  },
        };

        db.consumables.AddRange(consumables);
        db.equipments.AddRange(equipments);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ChestDB] TreasureChestDatabase 생성 완료: 소비형 {db.consumables.Count}개, 장비 {db.equipments.Count}개");
        EditorUtility.DisplayDialog("완료",
            $"TreasureChestDatabase.asset 생성!\n  소비형: {db.consumables.Count}개\n  장비:   {db.equipments.Count}개\n\n" +
            "Assets/Resources/TreasureChestDatabase.asset\n\n" +
            "Inspector에서 아이콘 스프라이트를 자유롭게 교체하세요.", "확인");
    }
}
#endif
