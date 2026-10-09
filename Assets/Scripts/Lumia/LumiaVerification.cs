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
            if (Debug.isDebugBuild && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-lumia-verify") >= 0)
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
                if(view=="lobby")LumiaGame.Instance.ExportPreviewLayoutAudit(Path.Combine(output,"preview-layout.json"));
                if(view=="irem_field" && !LumiaGame.Instance.Engine.SkillStateSnapshot(false).Any(t=>t.kind=="state" && t.label.Contains("고양이")))
                    throw new System.InvalidOperationException("Irem's cat state was not shown as a native state token.");
                if(view=="irem_reverted" && LumiaGame.Instance.Engine.SkillStateSnapshot(false).Any(t=>t.kind=="state" && t.label.Contains("고양이")))
                    throw new System.InvalidOperationException("Irem's cat state was not cleared by her second R use.");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, view + ".png"));
                yield return new WaitForSecondsRealtime(.6f);
                Debug.Log("LUMIA CAPTURE " + view);
            }
            Application.Quit();
        }
    }
}
