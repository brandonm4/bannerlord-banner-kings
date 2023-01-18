﻿using BannerKings.Managers.Items;
using HarmonyLib;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace BannerKings.Patches
{
    internal class FixesPatches
    {
        [HarmonyPatch(typeof(MobileParty))]
        internal class MobilePartyPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("OnRemoveParty")]
            private static bool OnRemovePartyPrefix(MobileParty __instance)
            {
                PartyComponent partyComponent = __instance.PartyComponent;
                if (partyComponent != null && partyComponent.MobileParty == null)
                {
                    AccessTools.Method((partyComponent as PartyComponent).GetType(), "SetMobilePartyInternal")
                        .Invoke(partyComponent, new object[] { __instance });
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(SiegeAftermathCampaignBehavior))]
        internal class SiegeAftermathCampaignBehaviorPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetSiegeAftermathInfluenceCost")]
            private static bool GetSiegeAftermathInfluenceCostPrefix(MobileParty attackerParty, Settlement settlement, 
                SiegeAftermathCampaignBehavior.SiegeAftermath aftermathType, ref float __result)
            {
                float result = 0f;
                if (attackerParty.Army != null && aftermathType != SiegeAftermathCampaignBehavior.SiegeAftermath.Pillage)
                {
                    int num = attackerParty.Army.Parties.Count((MobileParty t) =>
                    {
                        if (t.LeaderHero != null)
                        {
                            return t.LeaderHero.GetTraitLevel(DefaultTraits.Mercy) > 0;
                        }

                        return false;
                    });
                    int num2 = attackerParty.Army.Parties.Count((MobileParty t) => 
                    {
                        if (t.LeaderHero != null)
                        {
                            return t.LeaderHero.GetTraitLevel(DefaultTraits.Mercy) > 0;
                        }

                        return false;
                    });
                    if (aftermathType == SiegeAftermathCampaignBehavior.SiegeAftermath.Devastate)
                    {
                        result = settlement.Prosperity / 400f * (float)num;
                    }
                    else if (aftermathType == SiegeAftermathCampaignBehavior.SiegeAftermath.ShowMercy && attackerParty.MapFaction.Culture != settlement.Culture)
                    {
                        result = settlement.Prosperity / 400f * (float)num2;
                    }
                }
                __result = result;

                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryLogic))]
        internal class InventoryLogicPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("SlaughterItem")]
            private static bool SlaughterItemPrefix(ItemRosterElement itemRosterElement)
            {
                EquipmentElement equipmentElement = itemRosterElement.EquipmentElement;
                int meatCount = equipmentElement.Item.HorseComponent.MeatCount;
                int hideCount = equipmentElement.Item.HorseComponent.HideCount;

                if (meatCount == 0 || hideCount == 0)
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(DefaultItems))]
        internal class RegisterItemsAndCategories
        {
            [HarmonyPostfix]
            [HarmonyPatch("InitializeAll")]
            private static void InitializeAllPostfix()
            {
                BKItemCategories.Instance.Initialize();
                BKItems.Instance.Initialize();
            }
        }
    }
}
