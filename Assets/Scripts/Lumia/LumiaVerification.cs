using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in development-player screenshots; separate save path protects normal runs.</summary>
    public sealed class LumiaVerification : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartVerification()
        {
            if (Debug.isDebugBuild && (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-lumia-verify") >= 0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-camp-verify") >= 0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-strategy-verify") >= 0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-boss-verify") >= 0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-reward-verify") >= 0 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-boss-variety-verify") >= 0))
                new GameObject("Lumia verification").AddComponent<LumiaVerification>();
        }
        IEnumerator Start()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Verification"));
            Directory.CreateDirectory(output);
            yield return null;
            int skills=GameDatabase.Cards.Count(c=>c.category=="skill" && c.mechanics!=null);
            int passives=GameDatabase.Passives.Count(p=>p.mechanics!=null);
            int runes=GameDatabase.Runes.Count(r=>r.mechanics!=null);
            if(skills!=364 || passives!=91 || runes!=16) throw new System.InvalidOperationException("Missing identity profiles in native player.");
            Debug.Log("LUMIA IDENTITY skills="+skills+" passives="+passives+" runes="+runes);
            string[] views = { "lobby", "starting_passives", "preparation", "map", "long_map", "combat", "rewards", "rewards_cancelled", "combo", "detail", "fx_player", "fx_enemy", "fx_recover", "kiosk", "campfire", "encounter", "encounter_card", "encounter_detail", "encounter_trade", "encounter_risky", "encounter_long", "catalog", "inventory", "gear_inventory", "passive_inventory", "passive_replacement", "mechanic_blocks", "mechanic_discount", "mechanic_field", "mechanic_detail", "trait_basic", "trait_third", "trait_detail", "rune_buff", "rune_healing", "rune_inventory", "rune_detail", "rune_selection", "kiosk_coupon", "trait_revive", "enemy_detail", "combo_field", "status_cost", "status_lock", "status_field", "status_detail" };
            views=views.Concat(new[]{"preparation_pinned","detail_full","mechanic_detail_full","trait_detail_full","rune_detail_full","player_status","enemy_loadout","enemy_field","alex_weapon_deck","gear_detail","gear_detail_full"}).ToArray();
            views=views.Concat(new[]{"preview_hand","irem_detail","irem_detail_full","irem_field","irem_reverted"}).ToArray();
            views=views.Concat(new[]{"kiosk_unlocked","campfire_critical","campfire_tagged","campfire_empty","critical_detail","fx_critical","encounter_energy","help"}).ToArray();
            views=views.Concat(new[]{"debuff_status","debuff_bleed","debuff_burn","debuff_next_basic","debuff_delayed","debuff_multiple","debuff_conditional","debuff_enemy","debuff_summary","debuff_none"}).ToArray();
            views=views.Concat(new[]{"rewards_wildlife","rewards_wildlife_cancelled","wildlife_food_bag","encounter_basic","encounter_basic_detail"}).ToArray();
            views=views.Concat(new[]{"grouped_fiora","grouped_fiora_full","grouped_nia_full","grouped_rozzi_full","grouped_counter_full","grouped_rune_full"}).ToArray();
            views=views.Concat(new[]{"boss_preview_new","boss_preview_legacy","boss_preview_act2","jenny_passive_full"}).ToArray();
            views=views.Concat(new[]{"vf","bomb","oil","wound","displace","mobility","bloom","support"}
                .SelectMany(family=>new[]{"synergy_"+family+"_ready","synergy_"+family+"_used","synergy_"+family+"_full"}))
                .Concat(new[]{"synergy_vf_summary","synergy_bomb_summary"}).ToArray();
            views=views.Concat(new[]{"recast_cathy_cold","recast_cathy_denied","recast_cathy_ready","recast_cathy_granted","recast_cathy_spent","recast_cathy_detail_full",
                "recast_shoichi_w_full","recast_celine_w_full","recast_jan_e_full"}).ToArray();
            views=views.Concat(new[]{"audio_settings"}).ToArray();
            string[] campViews = { "campfire_choice", "campfire_craft_noheal", "campfire_rest", "campfire_rest_selected", "campfire_rest_cancelled", "campfire_rest_one_removed", "campfire_rest_removed", "campfire_rest_empty", "campfire_rest_protected", "campfire_rest_leave_selected", "campfire_rest_reentered", "campfire_tagged" };
            bool campOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-camp-verify") >= 0;
            views = campOnly ? campViews.Concat(new[] { "help" }).ToArray() : views.Concat(campViews.Where(view => !views.Contains(view))).ToArray();
            bool strategyOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-strategy-verify") >= 0;
            if (strategyOnly) views = new[] { "strategy_upgrade_offer", "strategy_upgrade_select", "strategy_upgrade_power", "strategy_upgrade_special", "strategy_upgrade_applied", "strategy_upgrade_reloaded",
                "strategy_retain_select", "strategy_retain_selected", "strategy_retain_cancelled", "strategy_retain_reloaded", "strategy_retain_after",
                "strategy_map_before", "strategy_map_scouted", "strategy_map_stock", "strategy_map_reloaded", "strategy_hyperloop_select", "strategy_hyperloop_cancelled", "strategy_hyperloop_used",
                "strategy_chain_promise", "strategy_chain_unavailable", "strategy_chain_pending", "strategy_chain_offers", "strategy_chain_followup", "strategy_chain_reloaded", "strategy_chain_completed", "help" };
            bool bossOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-boss-verify") >= 0;
            if (bossOnly)
            {
                LumiaGame.Instance.VerificationBossRoster();
                views = new[] { "boss_ui_badges", "boss_ui_goal_damage", "boss_ui_goal_block", "boss_ui_goal_basic", "boss_ui_goal_skill", "boss_ui_goal_movement", "boss_ui_goal_mixedkeys", "boss_ui_goal_cheapcards", "boss_ui_guide_bottom",
                    "boss_ui_phase_pending", "boss_ui_phase2", "boss_ui_nia_pending", "boss_ui_nia_phase3", "boss_ui_progress", "boss_ui_interrupted_reload",
                    "boss_ui_effect_strike", "boss_ui_effect_shield", "boss_ui_effect_delayedstrike", "boss_ui_effect_status", "boss_ui_effect_counter", "boss_ui_effect_drain", "boss_ui_effect_resourceprepare", "boss_ui_effect_cleanse", "boss_ui_effect_resolved", "help" };
            }
            bool rewardOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-reward-verify") >= 0;
            if (rewardOnly)
            {
                LumiaGame.Instance.VerificationRewardFlows();
                views = new[] { "reward_ui_subject_empty", "reward_ui_subject_two", "reward_ui_subject_replaced", "reward_ui_subject_reloaded", "reward_ui_subject_committed",
                    "reward_ui_boss_two", "reward_ui_boss_reloaded", "reward_ui_wildlife_budget", "help" };
            }
            bool varietyOnly = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-bts-boss-variety-verify") >= 0;
            if (varietyOnly)
            {
                LumiaGame.Instance.VerificationBossRoster();
                LumiaGame.Instance.VerificationBossVarietyFlows();
                views = new[] { "boss_variety_guide_debuffskill", "boss_variety_guide_nodebuffskill", "boss_variety_guide_noheavyhit", "boss_variety_guide_nomovement",
                    "boss_variety_compliant", "boss_variety_violation_debuff", "boss_variety_violation_heavy", "boss_variety_violation_movement", "boss_variety_violation_reloaded",
                    "boss_variety_turn_end_success", "boss_variety_one_card_failed", "boss_variety_debuff_used" };
            }
            foreach (string view in views)
            {
                LumiaGame.Instance.VerificationView(view);
                if (view == "rewards_wildlife" || view == "rewards_wildlife_cancelled" || view == "wildlife_food_bag")
                {
                    var engine = LumiaGame.Instance.Engine; var reward = engine.State.rewards;
                    if (reward.choices.Count != 4 || !reward.choices.Contains("basic_attack") || reward.foodId != "meat" || engine.State.foods.Count(id => id == "meat") != 1)
                        throw new System.InvalidOperationException("Wildlife bonus card or food missing in native UI.");
                    bool cancelled = view == "rewards_wildlife_cancelled";
                    if (reward.taken.Contains("basic_attack") == cancelled || engine.RewardRemainingBudget != (cancelled ? 3 : 2))
                        throw new System.InvalidOperationException("Wildlife basic attack selection/cancellation failed in native UI.");
                    var reloaded = new GameEngine(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(engine.State)));
                    if (reloaded.State.rewards.foodId != "meat" || reloaded.State.foods.Count(id => id == "meat") != 1 || !reloaded.State.rewards.choices.Contains("basic_attack"))
                        throw new System.InvalidOperationException("Wildlife pending reward did not survive Unity save serialization.");
                }
                if ((view == "encounter_basic" || view == "encounter_basic_detail") && !GameDatabase.Event(LumiaGame.Instance.Engine.State.chosenEventId).options.Any(option => EventPresentation.RewardCard(option)?.id == "basic_attack" && option.description.Contains("기본 공격")))
                    throw new System.InvalidOperationException("Named basic attack event reward missing in native UI.");
                if (view == "kiosk" && LumiaGame.Instance.Engine.IsKioskObjectUnlocked("blood"))
                    throw new System.InvalidOperationException("VF blood sample was unlocked before two bosses.");
                if (view == "kiosk_unlocked" && !LumiaGame.Instance.Engine.IsKioskObjectUnlocked("blood"))
                    throw new System.InvalidOperationException("VF blood sample remained locked after two bosses.");
                if(view=="mechanic_discount")
                {
                    var engine=LumiaGame.Instance.Engine;
                    if(engine.State.combat.energy!=0 || engine.EffectiveCardCost("nia_q")!=0 || !engine.CanPlayCard(0)) throw new System.InvalidOperationException("Nia zero-energy discount did not reach native UI.");
                    Debug.Log("LUMIA IDENTITY Nia zero-energy Q PASS");
                }
                if(view=="rune_healing" && !LumiaGame.Instance.Engine.State.combat.playerTraits.skills.effects.Any(x=>x.key=="healing_drone"))
                    throw new System.InvalidOperationException("Low-health healing drone did not reach the native UI.");
                if(view=="preparation_pinned")
                {
                    var s=LumiaGame.Instance.Engine.State;
                    if(s.draftSelected.Count!=1 || !s.draftOffers.Contains(s.draftSelected[0]) || s.draftOffers.Any(id=>GameDatabase.Card(id).cost>=5))
                        throw new System.InvalidOperationException("Pinned starting card / affordable starting offer missing in native UI.");
                }
                if(view=="enemy_loadout")
                {
                    var c=LumiaGame.Instance.Engine.State.combat;
                    if(c.enemyGear.Count<3 || string.IsNullOrEmpty(c.enemyPassiveId) || !c.enemyDeck.Contains("basic_attack") || !c.enemyDeck.Contains(c.enemyWeaponCardId) || !c.enemyDeck.Contains(c.enemyTacticalCardId))
                        throw new System.InvalidOperationException("Late boss loadout missing in native UI.");
                }
                if(view=="alex_weapon_deck" && LumiaGame.Instance.Engine.State.deck.Count(id=>GameDatabase.Card(id).category=="weapon")<4)
                    throw new System.InvalidOperationException("Alex's weapon-sourced additional D cards missing in native UI.");
                yield return new WaitForSecondsRealtime(view.StartsWith("fx_") || view=="trait_third" ? .25f : .6f);
                if (bossOnly && view == "boss_ui_badges") LumiaGame.Instance.VerificationBossHudLayout();
                if (varietyOnly && view == "boss_variety_guide_debuffskill") LumiaGame.Instance.VerificationBossHudLayout();
                if(view=="lobby")LumiaGame.Instance.ExportPreviewLayoutAudit(Path.Combine(output,"preview-layout.json"));
                if(view=="irem_field" && !LumiaGame.Instance.Engine.SkillStateSnapshot(false).Any(t=>t.kind=="state" && t.label.Contains("고양이")))
                    throw new System.InvalidOperationException("Irem's cat state was not shown as a native state token.");
                if(view=="irem_reverted" && LumiaGame.Instance.Engine.SkillStateSnapshot(false).Any(t=>t.kind=="state" && t.label.Contains("고양이")))
                    throw new System.InvalidOperationException("Irem's cat state was not cleared by her second R use.");
                if(view.StartsWith("synergy_",System.StringComparison.Ordinal)) LumiaGame.Instance.VerifySynergyFieldLayout(view);
                ScreenCapture.CaptureScreenshot(Path.Combine(output, view + ".png"));
                yield return new WaitForSecondsRealtime(.6f);
                Debug.Log("LUMIA CAPTURE " + view);
                if (strategyOnly) Debug.Log("BTS STRATEGY PASS " + view);
                if (bossOnly) Debug.Log("BTS BOSS PASS " + view);
                if (rewardOnly) Debug.Log("BTS REWARD PASS " + view);
                if (varietyOnly) Debug.Log("BTS BOSS VARIETY PASS " + view);
                if (campOnly) Debug.Log("BTS CAMP PASS " + view + " / hp=" + LumiaGame.Instance.Engine.State.hp + " / removals=" + LumiaGame.Instance.Engine.State.campRemovalsRemaining);
            }
            if (strategyOnly) Debug.Log("BTS STRATEGY VERIFICATION PASS / views=" + views.Length);
            if (bossOnly) Debug.Log("BTS BOSS VERIFICATION PASS / views=" + views.Length);
            if (rewardOnly) Debug.Log("BTS REWARD VERIFICATION PASS / views=" + views.Length);
            if (varietyOnly) Debug.Log("BTS BOSS VARIETY VERIFICATION PASS / views=" + views.Length);
            if (campOnly) Debug.Log("BTS CAMP VERIFICATION PASS / views=" + views.Length);
            Application.Quit();
        }
    }
}
