using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        static Color AugmentColor(AugmentTier tier)
        {
            return tier == AugmentTier.Silver ? C("c7d4e4") : tier == AugmentTier.Gold ? Gold : C("c69aef");
        }

        void OpenAugmentInventory() { inventory = true; inventoryTab = 5; inventoryScroll = Vector2.zero; }

        void DrawAugmentEmblem(Rect rect, AugmentTier tier)
        {
            Color tone = AugmentColor(tier); float unit = rect.width / 12;
            for (int row = 0; row < 12; row++)
            {
                int inset = row < 6 ? 5 - row : row - 6;
                Fill(new Rect(rect.x + inset * unit, rect.y + row * unit, (12 - inset * 2) * unit, unit), tone);
            }
            Fill(new Rect(rect.x + 4 * unit, rect.y + 3 * unit, 4 * unit, 6 * unit), Ink);
            Fill(new Rect(rect.x + 3 * unit, rect.y + 4 * unit, 6 * unit, 4 * unit), Ink);
            if (tier == AugmentTier.Silver)
                Fill(new Rect(rect.x + 5 * unit, rect.y + 4 * unit, 2 * unit, 4 * unit), tone);
            else if (tier == AugmentTier.Gold)
            {
                Fill(new Rect(rect.x + 4 * unit, rect.y + 5 * unit, 4 * unit, 2 * unit), tone);
                Fill(new Rect(rect.x + 5 * unit, rect.y + 4 * unit, 2 * unit, 4 * unit), tone);
            }
            else
            {
                Fill(new Rect(rect.x + 4 * unit, rect.y + 4 * unit, 2 * unit, 4 * unit), Mint);
                Fill(new Rect(rect.x + 6 * unit, rect.y + 4 * unit, 2 * unit, 4 * unit), Pink);
                Fill(new Rect(rect.x + 5 * unit, rect.y + 5 * unit, 2 * unit, 2 * unit), Text);
            }
        }

        static string AugmentStackText(EndlessAugmentDef definition, int stacks, bool offer)
        {
            if (EndlessAugments.IsSupport(definition))
                return offer ? "즉시 지원 · 선택을 확정하면 한 번 지급됩니다." : "즉시 지원 · " + stacks + "회 지급 완료";
            return offer ? "지속 효과 · 현재 중첩 " + stacks + " / " + definition.maxStacks + "\n획득 후 중첩 " + (stacks + 1) + " / " + definition.maxStacks
                : "지속 효과 · 현재 중첩 " + stacks + " / " + definition.maxStacks;
        }

        void DrawAugmentChoice()
        {
            var offers = Engine.AugmentOffers;
            var tier = offers.Count == 0 ? AugmentTier.Silver : offers[0].tier;
            Header("증강 선택 / " + EndlessAugments.TierName(tier), RunModes.CycleLabel(Engine.State) + " 완료 · 증강 이후 장비 한도를 늘릴 부위를 선택합니다.");
            Txt(new Rect(64, 112, 1152, 32), "이번 실험의 결과를, 다음 실험의 힘으로.", 23, AugmentColor(tier));
            Txt(new Rect(64, 151, 1152, 22), "다른 증강을 누르면 선택이 바뀌고, 선택한 증강을 다시 누르면 취소됩니다. 확정하기 전에는 효과를 얻지 않습니다.", 13, Muted);
            for (int index = 0; index < offers.Count && index < 3; index++)
            {
                var definition = offers[index]; int stacks = Engine.AugmentStacks(definition.id);
                bool selected = Engine.SelectedAugmentId == definition.id;
                var card = new Rect(64 + index * 388, 187, 376, 375); Color tone = AugmentColor(definition.tier);
                Box(card, selected ? C("29314a") : Panel, selected ? tone : Line);
                Txt(new Rect(card.x + 20, card.y + 17, 336, 24), "0" + (index + 1) + " / " + EndlessAugments.TierName(definition.tier), 14, tone);
                DrawAugmentEmblem(new Rect(card.x + 20, card.y + 62, 48, 48), definition.tier);
                Para(new Rect(card.x + 86, card.y + 61, 270, 58), definition.name, 20, Text);
                Para(new Rect(card.x + 20, card.y + 137, 336, 43), AugmentStackText(definition, stacks, true), 12, Muted);
                Txt(new Rect(card.x + 20, card.y + 179, 336, 17), EndlessAugments.IsSupport(definition) ? "즉시 지급" : "획득 후 효과", 11, tone);
                Para(new Rect(card.x + 20, card.y + 201, 336, 116), EndlessAugments.Description(definition.id, EndlessAugments.IsSupport(definition) ? 1 : stacks + 1), 14, Text);
                Txt(new Rect(card.x + 20, card.y + 332, 336, 23), selected ? "선택됨 · 다시 눌러 취소" : "눌러서 선택", 13, selected ? tone : Muted, true);
                if (selected) Fill(new Rect(card.x + 2, card.yMax - 5, card.width - 4, 3), tone);
                if (Hit(card)) ToggleAugmentChoice(definition.id);
            }
            Para(new Rect(64, 581, 1152, 35), "지속 증강은 원정이 끝날 때까지 유지되며 같은 증강은 최대 3중첩입니다. 메뉴에서 저장하고 로비로 돌아가도 후보와 선택은 유지됩니다.", 13, Muted);
            if (Btn(new Rect(64, 637, 376, 45), "증강 포기 / 장비 부위 선택  >", false, Muted)) FinishAugmentChoice(true);
            var chosen = EndlessAugments.Find(Engine.SelectedAugmentId);
            if (Btn(new Rect(456, 637, 760, 45), chosen == null ? "증강을 선택하세요" : chosen.name + " 획득 / 장비 부위 선택  >", true, AugmentColor(tier), chosen != null)) FinishAugmentChoice(false);
        }

        bool ToggleAugmentChoice(string id) { return Act(() => Engine.ToggleAugment(id)); }

        bool FinishAugmentChoice(bool skip)
        {
            bool result = Act(() => skip ? Engine.SkipAugment() : Engine.ConfirmAugment(), skip ? "ui.click" : "level.up");
            if (result) { mapAct = -1; mapRow = -99; ResetBossPreviews(); effects.Clear(); Engine.CombatActions.Clear(); }
            return result;
        }

        void DrawEquipmentSlotChoice()
        {
            Header("장비 부위 확장", "무한 "+Engine.EquipmentExpansionCycle+"주기 완료 · 한 부위의 장착 한도를 1 늘린 뒤 다음 주기로 진행합니다.");
            Txt(new Rect(64,117,1152,34),"어느 부위의 장비를 더 장착할까요?",23,Gold);
            Para(new Rect(64,164,1152,39),"다른 부위를 누르면 선택이 바뀌고, 선택한 부위를 다시 누르면 취소됩니다. 확정 전에는 장착 한도와 주기가 바뀌지 않습니다.",13,Muted);
            for(int index=0;index<5;index++)
            {
                var slot=(GearSlot)index;int capacity=Engine.EquipmentCapacity(slot);
                bool selected=Engine.SelectedEquipmentSlotIndex==index,available=capacity<int.MaxValue;
                Rect card=new Rect(64+index*232,224,224,294);
                Box(card,selected?C("29314a"):Panel,selected?Gold:Line);
                Txt(new Rect(card.x+15,card.y+18,194,30),SlotName(slot),20,Gold,true);
                var example=GameDatabase.Gear.FirstOrDefault(gear=>gear.slot==slot);
                if(example!=null)Tex(new Rect(card.x+80,card.y+63,64,64),PixelArt.SystemIcon(example.id),ScaleMode.ScaleToFit);
                int held=Engine.State.gear.Count(id=>GameDatabase.Equipment(id).slot==slot);
                Txt(new Rect(card.x+15,card.y+144,194,24),"현재 장착 "+held+" / "+capacity,13,Muted,true);
                Txt(new Rect(card.x+15,card.y+184,194,30),capacity+" → "+((long)capacity+1)+"개",capacity<10000?20:12,selected?Gold:Text,true);
                Txt(new Rect(card.x+15,card.y+248,194,24),!available?"한도 확장 불가":selected?"선택됨 · 다시 눌러 취소":"눌러서 선택",12,selected?Gold:Muted,true);
                if(Hit(card,available))ToggleEquipmentSlotChoice(slot);
            }
            Para(new Rect(64,551,1152,56),"확장한 한도는 이번 무한 원정 동안 유지됩니다. 장비와 부위별 한도는 저장되며, 메뉴에서 로비로 돌아와도 현재 선택을 이어갈 수 있습니다.",14,Muted);
            int chosen=Engine.SelectedEquipmentSlotIndex;
            bool valid=chosen>=0&&chosen<5&&Engine.EquipmentCapacity((GearSlot)chosen)<int.MaxValue;
            if(Btn(new Rect(64,637,1152,45),valid?SlotName((GearSlot)chosen)+" 장착 한도 +1 / 다음 주기  >":"한도를 늘릴 장비 부위를 선택하세요",true,Gold,valid))FinishEquipmentSlotChoice();
        }

        bool ToggleEquipmentSlotChoice(GearSlot slot) => Act(()=>Engine.ToggleEquipmentSlot(slot));

        bool FinishEquipmentSlotChoice()
        {
            bool result=Act(Engine.ConfirmEquipmentSlot,"level.up");
            if(result){mapAct=-1;mapRow=-99;ResetBossPreviews();effects.Clear();Engine.CombatActions.Clear();}
            return result;
        }

        void DrawOwnedAugments()
        {
            var groups = Engine.State.augments.Where(id => EndlessAugments.Find(id) != null).GroupBy(id => id).ToArray();
            Txt(new Rect(83, 204, 1090, 27), RunModes.CycleLabel(Engine.State) + " / 총 " + Engine.AugmentCount + "회 획득 · 지속 효과와 지급된 지원 기록", 14, AugmentColor(AugmentTier.Prismatic));
            if (groups.Length == 0)
            {
                DrawAugmentEmblem(new Rect(603, 295, 72, 72), AugmentTier.Silver);
                Para(new Rect(250, 397, 780, 88), "아직 획득한 증강이 없습니다.\n무한 모드에서 3막의 니아를 처치하고 보상과 패시브 선택을 마치면 증강을 선택합니다.\n첫 세 번은 "+EndlessAugments.TierName(AugmentTier.Silver)+", "+EndlessAugments.TierName(AugmentTier.Gold)+", "+EndlessAugments.TierName(AugmentTier.Prismatic)+" 등급 순서로 등장합니다.", 16, Muted);
                return;
            }
            inventoryScroll = GUI.BeginScrollView(new Rect(80, 245, 1120, 386), inventoryScroll, new Rect(0, 0, 1090, Math.Max(386, groups.Length * 132)));
            for (int index = 0; index < groups.Length; index++)
            {
                var definition = EndlessAugments.Find(groups[index].Key); int stacks = groups[index].Count();
                var row = new Rect(0, index * 132, 1083, 120); Color tone = AugmentColor(definition.tier);
                Box(row, Panel, Line); DrawAugmentEmblem(new Rect(row.x + 17, row.y + 26, 48, 48), definition.tier);
                Txt(new Rect(row.x + 82, row.y + 12, 660, 28), definition.name, 19, tone);
                Txt(new Rect(row.x + 760, row.y + 16, 300, 22), EndlessAugments.TierName(definition.tier), 12, tone, true);
                Para(new Rect(row.x + 82, row.y + 50, 978, 45), EndlessAugments.Description(definition.id, stacks), 13, Text);
                Txt(new Rect(row.x + 82, row.y + 99, 978, 17), AugmentStackText(definition, stacks, false), 11, Muted);
            }
            GUI.EndScrollView();
        }

        bool AugmentVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-augment-verify") >= 0;

        public string[] AugmentVerificationViews => new[]
        {
            "augment_silver", "augment_selected", "augment_cancelled", "augment_replaced", "augment_saved_menu", "augment_reloaded", "augment_empty_inventory", "augment_confirmed_map",
            "augment_owned", "augment_gold", "augment_prismatic", "augment_skipped_map", "augment_support", "augment_support_owned", "augment_all_owned", "augment_all_scrolled"
            ,"equipment_choice","equipment_selected","equipment_cancelled","equipment_replaced","equipment_reloaded","equipment_confirmed_map",
            "equipment_expanded_inventory","equipment_expanded_scrolled","equipment_replacement","equipment_replacement_scrolled",
            "classification_catalog","classification_detail","classification_full","classification_help"
        };

        void GuardAugmentVerification()
        {
            if (!AugmentVerificationEnabled || Path.GetFileName(savePath) != "lumia-verification.json")
                throw new InvalidOperationException("Augment verification requires the isolated development run and research slots.");
        }

        void PrepareAugmentVerification(int completedCycles = 0)
        {
            GuardAugmentVerification(); ResetModesProfile(true, true);
            Engine = ModesFixtureRun(80301, RunMode.Endless); lobby = false;
            for (int cycle = 0; cycle < completedCycles; cycle++)
            {
                for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1, false);
                if (!FinishAugmentChoice(true)||!CompleteEquipmentVerificationChoice(GearSlot.Weapon)) throw new InvalidOperationException("Augment fixture could not explicitly finish its completed cycle.");
            }
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1, false);
            effects.Clear(); Engine.CombatActions.Clear(); toastUntil = 0;
        }

        public void VerificationAugmentFlow()
        {
            if (!AugmentVerificationEnabled) return;
            GuardAugmentVerification(); VerificationModesFlow(); PrepareAugmentVerification();
            if (Engine.State.stage != RunStage.AugmentChoice || Engine.State.endlessCycle != 0 || Engine.AugmentOffers.Count != 3
                || Engine.AugmentOffers.Select(offer => offer.id).Distinct().Count() != 3 || Engine.AugmentOffers.Any(offer => offer.tier != AugmentTier.Silver))
                throw new InvalidOperationException("The actual third boss must offer three distinct Silver augments before the next cycle begins.");
            string[] candidates = Engine.State.augmentOffers.ToArray(); int offeringCycle = Engine.State.augmentOfferingCycle, rng = Engine.State.rngState;
            Save(); int originalCount = Engine.AugmentCount;
            if (Engine.ConfirmAugment() || Engine.State.stage != RunStage.AugmentChoice || Engine.AugmentCount != originalCount)
                throw new InvalidOperationException("An unselected augment cannot advance the cycle or grant an effect.");
            if (!ToggleAugmentChoice(candidates[0]) || Engine.SelectedAugmentId != candidates[0] || Engine.AugmentCount != originalCount || Engine.State.endlessCycle != 0)
                throw new InvalidOperationException("Selecting a candidate must only mark it and save without granting it.");
            if (!ToggleAugmentChoice(candidates[0]) || !string.IsNullOrEmpty(Engine.SelectedAugmentId)) throw new InvalidOperationException("Clicking the selected augment must cancel it.");
            if (!ToggleAugmentChoice(candidates[0]) || !ToggleAugmentChoice(candidates[1]) || Engine.SelectedAugmentId != candidates[1]
                || Engine.State.rngState != rng || !candidates.SequenceEqual(Engine.State.augmentOffers))
                throw new InvalidOperationException("Changing one selected augment must retain the same candidates and gameplay RNG.");
            string selectedState = JsonUtility.ToJson(Engine.State);
            OpenAugmentInventory(); inventory = false; OpenRunMenu(); SaveAndReturnToLobby();
            if (selectedState != JsonUtility.ToJson(Engine.State)) throw new InvalidOperationException("Opening the augment inventory or returning to the lobby must not mutate the pending run.");
            Load();
            if (lobby || Engine.State.stage != RunStage.AugmentChoice || Engine.SelectedAugmentId != candidates[1]
                || Engine.State.augmentOfferingCycle != offeringCycle || !candidates.SequenceEqual(Engine.State.augmentOffers)
                || Engine.State.rngState != rng || Engine.AugmentCount != originalCount)
                throw new InvalidOperationException("Saving and returning to the lobby must preserve the pending augment and selected candidate through Unity reload.");
            if (!FinishAugmentChoice(false) || Engine.State.stage != RunStage.EquipmentSlotChoice || Engine.State.endlessCycle != 0 || Engine.AugmentCount != originalCount + 1
                || Engine.AugmentStacks(candidates[1]) != 1 || Engine.SelectedEquipmentSlotIndex!=-1 || Engine.EquipmentExpansionCycle!=1)
                throw new InvalidOperationException("Augment confirmation must grant exactly one augment, then wait for the equipment choice before the next cycle.");
            if(Engine.ConfirmEquipmentSlot() || Engine.State.stage!=RunStage.EquipmentSlotChoice || Engine.EquipmentCapacity(GearSlot.Clothes)!=2)
                throw new InvalidOperationException("An unselected equipment choice cannot expand capacity or advance the cycle.");
            int slotRng=Engine.State.rngState;
            if(!ToggleEquipmentSlotChoice(GearSlot.Weapon)||Engine.SelectedEquipmentSlotIndex!=(int)GearSlot.Weapon||Engine.EquipmentCapacity(GearSlot.Weapon)!=2
                ||!ToggleEquipmentSlotChoice(GearSlot.Weapon)||Engine.SelectedEquipmentSlotIndex!=-1
                ||!ToggleEquipmentSlotChoice(GearSlot.Weapon)||!ToggleEquipmentSlotChoice(GearSlot.Clothes)||Engine.SelectedEquipmentSlotIndex!=(int)GearSlot.Clothes
                ||Engine.State.rngState!=slotRng||Engine.State.endlessCycle!=0)
                throw new InvalidOperationException("Equipment selection must change and cancel without granting a slot, advancing the cycle or consuming RNG.");
            string pendingSlots=JsonUtility.ToJson(Engine.State);
            OpenRunMenu();SaveAndReturnToLobby();Load();
            if(lobby||Engine.State.stage!=RunStage.EquipmentSlotChoice||Engine.SelectedEquipmentSlotIndex!=(int)GearSlot.Clothes
                ||Engine.EquipmentExpansionCycle!=1||Engine.State.endlessCycle!=0||Engine.State.rngState!=slotRng
                ||Engine.EquipmentCapacity(GearSlot.Clothes)!=2||Engine.AugmentCount!=originalCount+1||pendingSlots!=JsonUtility.ToJson(Engine.State))
                throw new InvalidOperationException("A pending equipment selection must survive the actual Unity save / lobby / reload flow unchanged.");
            if(!FinishEquipmentSlotChoice()||Engine.State.stage!=RunStage.Map||Engine.State.endlessCycle!=1||Engine.State.act!=1
                ||Engine.State.archivedMapNodes.Count==0||Engine.EquipmentCapacity(GearSlot.Clothes)!=3||Engine.EquipmentCapacity(GearSlot.Weapon)!=2
                ||Engine.ConfirmEquipmentSlot())
                throw new InvalidOperationException("Equipment confirmation must expand only the selected body slot once and then generate the next cycle.");
            if (Engine.ConfirmAugment() || Engine.SkipAugment() || Engine.AugmentCount != originalCount + 1 || Engine.State.endlessCycle != 1)
                throw new InvalidOperationException("Augment confirmation and skipping cannot be repeated after the cycle has advanced.");
            Save(); Load();
            if (Engine.AugmentCount != originalCount + 1 || Engine.AugmentStacks(candidates[1]) != 1 || Engine.State.endlessCycle != 1||Engine.EquipmentCapacity(GearSlot.Clothes)!=3)
                throw new InvalidOperationException("Owned augments and expanded slots must survive Unity save and reload without being granted twice.");
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1, false);
            if (Engine.State.stage != RunStage.AugmentChoice || Engine.AugmentOffers.Any(offer => offer.tier != AugmentTier.Gold))
                throw new InvalidOperationException("The second completed cycle must offer Gold augments.");
            if (!ToggleAugmentChoice(Engine.AugmentOffers[0].id) || !FinishAugmentChoice(false)||!CompleteEquipmentVerificationChoice(GearSlot.Weapon) || Engine.State.endlessCycle != 2 || Engine.AugmentCount != originalCount + 2)
                throw new InvalidOperationException("The second cycle must grant one chosen Gold augment.");
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1, false);
            if (Engine.State.stage != RunStage.AugmentChoice || Engine.AugmentOffers.Any(offer => offer.tier != AugmentTier.Prismatic))
                throw new InvalidOperationException("The third completed cycle must offer Prismatic augments.");
            if (!FinishAugmentChoice(true) || Engine.State.stage!=RunStage.EquipmentSlotChoice||Engine.State.endlessCycle!=2
                ||!CompleteEquipmentVerificationChoice(GearSlot.Weapon)||Engine.EquipmentCapacity(GearSlot.Weapon)!=4
                || Engine.State.endlessCycle != 3 || Engine.AugmentCount != originalCount + 2 || Engine.State.stage != RunStage.Map)
                throw new InvalidOperationException("Skipping the augment must still require one equipment expansion before advancing without an augment.");
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1, false);
            if(!FinishAugmentChoice(true)||Engine.State.stage!=RunStage.EquipmentSlotChoice)throw new InvalidOperationException("Pending equipment retirement fixture failed.");
            OpenRunMenu(); runMenu = false; endlessConfirm = true;
            if (!ConfirmEndlessRetirement() || !Engine.State.endlessRetired || Engine.State.stage != RunStage.Lost || Engine.State.research?.settled != true)
                throw new InvalidOperationException("The pending equipment screen must still allow the ongoing Endless expedition to be settled.");
            inventory = false; modeSelecting = runMenu = endlessConfirm = false; effects.Clear(); toastUntil = 0;
            Debug.Log("BTS AUGMENT FLOW PASS / actualBosses=12 / threeTiers=PASS / pendingBeforeNextCycle=PASS / selectionToggleReplace=PASS / savedCandidates=PASS / unityReload=PASS / acquireOnce=PASS / skip=PASS / retirementPending=PASS / equipmentChoice=PASS / equipmentToggleReplace=PASS / equipmentPendingUnityReload=PASS / equipmentAcquireOnce=PASS / equipmentRepeatExpansion=PASS / modesRegression=PASS / isolatedSave=PASS");
        }

        bool CompleteEquipmentVerificationChoice(GearSlot slot)
        {
            return Engine.State.stage==RunStage.EquipmentSlotChoice
                && (Engine.SelectedEquipmentSlotIndex==(int)slot||ToggleEquipmentSlotChoice(slot))&&FinishEquipmentSlotChoice();
        }

        public void VerificationAugmentLayout()
        {
            if (!AugmentVerificationEnabled) return;
            if (!initialized) throw new InvalidOperationException("Augment layout audit needs the actual native GUI font.");
            int measurements = 0; float choiceMaximum = 0, ownedMaximum = 0;
            Action<string, int, float, float> measure = (text, size, width, height) =>
            {
                font.RequestCharactersInTexture(text, size, FontStyle.Normal);
                float actual = new GUIStyle(wrapped) { fontSize = size }.CalcHeight(new GUIContent(text), width); measurements++;
                if (actual > height + .1f) throw new InvalidOperationException("Augment text exceeds native layout: " + text + " / " + actual + " > " + height);
            };
            foreach (var definition in EndlessAugments.All)
            {
                measure(definition.name, 20, 270, 58);
                string description = EndlessAugments.Description(definition.id);
                measure(description, 14, 336, 116);
                choiceMaximum = Math.Max(choiceMaximum, new GUIStyle(wrapped) { fontSize = 14 }.CalcHeight(new GUIContent(description), 336));
                string stackedChoice = EndlessAugments.Description(definition.id, EndlessAugments.IsSupport(definition) ? 1 : definition.maxStacks);
                measure(stackedChoice, 14, 336, 116);
                choiceMaximum = Math.Max(choiceMaximum, new GUIStyle(wrapped) { fontSize = 14 }.CalcHeight(new GUIContent(stackedChoice), 336));
                foreach (int stacks in new[] { 1, 3 })
                {
                    string owned = EndlessAugments.Description(definition.id, stacks); measure(owned, 13, 978, 45);
                    ownedMaximum = Math.Max(ownedMaximum, new GUIStyle(wrapped) { fontSize = 13 }.CalcHeight(new GUIContent(owned), 978));
                    measure(AugmentStackText(definition, stacks, false), 11, 978, 17);
                }
                measure(AugmentStackText(definition, 2, true), 12, 336, 43);
            }
            measure("지속 증강은 원정이 끝날 때까지 유지되며 같은 증강은 최대 3중첩입니다. 메뉴에서 저장하고 로비로 돌아가도 후보와 선택은 유지됩니다.", 13, 1152, 35);
            measure("다른 부위를 누르면 선택이 바뀌고, 선택한 부위를 다시 누르면 취소됩니다. 확정 전에는 장착 한도와 주기가 바뀌지 않습니다.",13,1152,39);
            measure("확장한 한도는 이번 무한 원정 동안 유지됩니다. 장비와 부위별 한도는 저장되며, 메뉴에서 로비로 돌아와도 현재 선택을 이어갈 수 있습니다.",14,1152,56);
            foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot)))
            {
                measure(SlotName(slot),20,194,30);
                measure("선택됨 · 다시 눌러 취소",12,194,24);
            }
            foreach(var card in GameDatabase.Cards)
            {
                string classification=CardPresentation.ClassificationLabel(card);
                measure(classification,10,121,19);
                if(new GUIStyle(label){fontSize=10}.CalcSize(new GUIContent(classification)).x>121)
                    throw new InvalidOperationException("Card classification exceeds the existing smallest footer: "+card.id+" / "+classification);
            }
            Debug.Log("BTS AUGMENT LAYOUT PASS / measurements=" + measurements + " / definitions=" + EndlessAugments.All.Count + " / choiceMax=" + choiceMaximum + " / ownedMax=" + ownedMaximum + " / equipmentSlots=5 / classificationFooters="+GameDatabase.Cards.Count+" / minFooterWidth=121 / nativeFont=PASS");
        }

        public void VerificationAugmentView(string view)
        {
            if (!AugmentVerificationEnabled) return;
            GuardAugmentVerification();
            lobby = catalog = inventory = settings = help = fieldInfo = enemyLoadout = research = false;
            modeSelecting = runMenu = endlessConfirm = false; inspectCard = inspectTrait = inspectGear = craftPending = mapIntel = inspectBoss = "";
            effects.Clear(); toastUntil = 0; mapAct = -1; mapRow = -99; ResetBossPreviews();
            int cycles = view == "augment_gold" ? 1 : view == "augment_prismatic" ? 2 : 0;
            PrepareAugmentVerification(cycles);
            if (view == "augment_prismatic")
            {
                Engine.State.augmentOffers = EndlessAugments.All.Where(definition => definition.tier == AugmentTier.Prismatic && !EndlessAugments.IsSupport(definition))
                    .OrderByDescending(definition => EndlessAugments.Description(definition.id, definition.maxStacks).Length).Take(3).Select(definition => definition.id).ToList();
                var stackable = Engine.AugmentOffers[0];
                Engine.State.augments.AddRange(Enumerable.Repeat(stackable.id, 2));
            }
            if (view == "augment_support" || view == "augment_support_owned")
                Engine.State.augmentOffers = EndlessAugments.All.Where(definition => definition.tier == AugmentTier.Silver && EndlessAugments.IsSupport(definition)).Select(definition => definition.id).ToList();
            if (view == "augment_selected" || view == "augment_reloaded" || view == "augment_saved_menu" || view == "augment_confirmed_map" || view == "augment_owned" || view == "augment_support_owned")
                ToggleAugmentChoice(Engine.AugmentOffers[0].id);
            if (view == "augment_cancelled") { string id = Engine.AugmentOffers[0].id; ToggleAugmentChoice(id); ToggleAugmentChoice(id); }
            if (view == "augment_replaced") { ToggleAugmentChoice(Engine.AugmentOffers[0].id); ToggleAugmentChoice(Engine.AugmentOffers[1].id); }
            if (view == "augment_saved_menu") OpenRunMenu();
            if (view == "augment_reloaded") { SaveAndReturnToLobby(); Load(); }
            if (view == "augment_empty_inventory") OpenAugmentInventory();
            if (view == "augment_confirmed_map" || view == "augment_owned" || view == "augment_support_owned")
            {
                if (!FinishAugmentChoice(false)||!CompleteEquipmentVerificationChoice(GearSlot.Weapon)) throw new InvalidOperationException("Native augment confirmation fixture failed.");
                if (view != "augment_confirmed_map") OpenAugmentInventory();
            }
            if (view == "augment_skipped_map" && (!FinishAugmentChoice(true)||!CompleteEquipmentVerificationChoice(GearSlot.Weapon))) throw new InvalidOperationException("Native augment skip fixture failed.");
            if (view == "augment_all_owned" || view == "augment_all_scrolled")
            {
                if (!FinishAugmentChoice(true)||!CompleteEquipmentVerificationChoice(GearSlot.Weapon)) throw new InvalidOperationException("Native inventory fixture could not reach its next cycle.");
                Engine.State.augments = EndlessAugments.All.SelectMany(definition => Enumerable.Repeat(definition.id, EndlessAugments.IsSupport(definition) ? 2 : 3)).ToList();
                OpenAugmentInventory();
                if (view == "augment_all_scrolled") inventoryScroll = new Vector2(0, 10000);
            }
            if(view.StartsWith("equipment_",StringComparison.Ordinal))
            {
                if(!FinishAugmentChoice(true))throw new InvalidOperationException("Native equipment choice fixture could not reach the pending selection.");
                if(view=="equipment_selected"||view=="equipment_reloaded"||view=="equipment_confirmed_map")ToggleEquipmentSlotChoice(GearSlot.Weapon);
                if(view=="equipment_cancelled"){ToggleEquipmentSlotChoice(GearSlot.Head);ToggleEquipmentSlotChoice(GearSlot.Head);}
                if(view=="equipment_replaced"){ToggleEquipmentSlotChoice(GearSlot.Weapon);ToggleEquipmentSlotChoice(GearSlot.Legs);}
                if(view=="equipment_reloaded"){SaveAndReturnToLobby();Load();}
                if(view=="equipment_confirmed_map"&&!FinishEquipmentSlotChoice())throw new InvalidOperationException("Native equipment confirmation failed.");
                if(view=="equipment_expanded_inventory"||view=="equipment_expanded_scrolled"||view=="equipment_replacement"||view=="equipment_replacement_scrolled")
                {
                    if(!CompleteEquipmentVerificationChoice(GearSlot.Weapon))throw new InvalidOperationException("Native expanded equipment fixture failed.");
                    Engine.State.gearSlotBonuses=new List<int>{7,1,1,1,1};Engine.State.gear.Clear();
                    for(int index=0;index<5;index++)
                    {
                        var slot=(GearSlot)index;var equipment=GameDatabase.Gear.Where(gear=>gear.slot==slot).ToArray();
                        for(int held=0;held<Engine.EquipmentCapacity(slot);held++)Engine.State.gear.Add(equipment[held%equipment.Length].id);
                    }
                    Save();Load();
                    if(Engine.EquipmentCapacity(GearSlot.Weapon)!=9||Engine.State.gear.Count!=21)throw new InvalidOperationException("Expanded native equipment inventory did not survive Unity reload.");
                    if(view.StartsWith("equipment_expanded_",StringComparison.Ordinal))
                    {inventory=true;inventoryTab=1;inventoryScroll=view=="equipment_expanded_scrolled"?new Vector2(0,10000):Vector2.zero;}
                    else
                    {
                        var replacement=GameDatabase.Gear.First(gear=>gear.slot==GearSlot.Weapon);
                        Engine.State.stage=RunStage.Campfire;Engine.State.campChoice=2;Engine.State.campActions=3;Engine.State.objects.Add(replacement.objectId);
                        craftPending=replacement.id;craftReplacementScroll=view=="equipment_replacement_scrolled"?new Vector2(0,10000):Vector2.zero;
                    }
                }
            }
            if(view=="classification_catalog"){catalog=true;catalogQuery="이동";catalogPage=0;}
            if(view=="classification_detail"||view=="classification_full")
            {inspectCard=GameDatabase.Cards.OrderByDescending(card=>CardPresentation.ClassificationLabel(card).Length).First().id;summaryMode=view!="classification_full";detailScroll=Vector2.zero;}
            if(view=="classification_help"){help=true;helpScroll=new Vector2(0,10000);}
            toastUntil = 0; effects.Clear(); Engine.CombatActions.Clear();
        }
    }
}
