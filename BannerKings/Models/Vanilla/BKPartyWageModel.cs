﻿using BannerKings.Managers.Education.Lifestyles;
using BannerKings.Managers.Institutions.Religions;
using BannerKings.Managers.Skills;
using BannerKings.Managers.Titles.Laws;
using BannerKings.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace BannerKings.Models.Vanilla
{
    public class BKPartyWageModel : DefaultPartyWageModel
    {
        public override int GetCharacterWage(CharacterObject character)
        {
            var result = character.Tier switch
            {
                0 => 1,
                1 => 2,
                2 => 4,
                3 => 6,
                4 => 16,
                5 => 32,
                6 => 45,
                _ => 60
            };

            return result;
        }

        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions = false)
        {
            var result = base.GetTotalWage(mobileParty, includeDescriptions);
            var leader = mobileParty.LeaderHero ?? mobileParty.Owner;
            if (leader != null)
            {
                var totalCulture = 0f;
                var mountedTroops = 0f;
                for (var i = 0; i < mobileParty.MemberRoster.Count; i++)
                {
                    var elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(i);
                    if (elementCopyAtIndex.Character.Culture == leader.Culture)
                    {
                        totalCulture += elementCopyAtIndex.Number;
                    }

                    if (elementCopyAtIndex.Character.HasMount())
                    {
                        mountedTroops += elementCopyAtIndex.Number;
                    }

                    if (elementCopyAtIndex.Character.IsHero)
                    {
                        if (elementCopyAtIndex.Character.HeroObject == mobileParty.LeaderHero)
                        {
                            continue;
                        }

                        var skills = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
                        var companionModel = new BKCompanionPrices();
                        var totalCost = 0f;
                        foreach (var skill in skills)
                        {
                            float skillValue = elementCopyAtIndex.Character.GetSkillValue(skill);
                            if (skillValue > 30)
                            {
                                totalCost += skillValue * companionModel.GetCostFactor(skill);
                            }
                        }

                        result.Add(totalCost * 0.005f, elementCopyAtIndex.Character.Name);
                    }
                }

                var proportion = MBMath.ClampFloat(totalCulture / mobileParty.MemberRoster.TotalManCount, 0f, 1f);
                if (proportion > 0f)
                {
                    result.AddFactor(proportion * -0.1f, GameTexts.FindText("str_culture"));
                }

                if (mobileParty.IsGarrison)
                {
                    result.Add(result.ResultNumber * -0.5f);
                }

                var education = BannerKingsConfig.Instance.EducationManager.GetHeroEducation(leader);
                float mountedProportion = mountedTroops / mobileParty.MemberRoster.Count;
                if (education.HasPerk(BKPerks.Instance.CataphractEquites) && mountedTroops > 0f)
                {
                    result.AddFactor(mountedProportion * -0.1f, BKPerks.Instance.CataphractEquites.Name);
                }

                if (mobileParty.SiegeEvent != null && education.Lifestyle != null && 
                    education.Lifestyle.Equals(DefaultLifestyles.Instance.SiegeEngineer))
                {
                    result.AddFactor(-0.3f, DefaultLifestyles.Instance.SiegeEngineer.Name);
                }
            }

            if (mobileParty.IsCaravan && mobileParty.Owner != null)
            {
                var education = BannerKingsConfig.Instance.EducationManager.GetHeroEducation(mobileParty.Owner);
                if (education.HasPerk(BKPerks.Instance.CaravaneerDealer))
                {
                    result.AddFactor(-0.1f, BKPerks.Instance.CaravaneerDealer.Name);
                }

                result.AddFactor(-0.25f, GameTexts.FindText("str_party_type", "Caravan"));
            }

            return result;
        }

        public override int GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            var result = new ExplainedNumber(base.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost) * 1.4f);
            result.LimitMin(GetCharacterWage(troop) * 2f);

            ExceptionUtils.TryCatch(() =>
            {
                if (buyerHero != null)
                {
                    if (buyerHero.CurrentSettlement != null)
                    {
                        var title = BannerKingsConfig.Instance.TitleManager.GetTitle(buyerHero.CurrentSettlement);
                        if (title != null)
                        {
                            var contract = title.contract;
                            if (contract.IsLawEnacted(DefaultDemesneLaws.Instance.DraftingFreeContracts))
                            {
                                result.AddFactor(1f, DefaultDemesneLaws.Instance.DraftingFreeContracts.Name);
                            }
                            else if (contract.IsLawEnacted(DefaultDemesneLaws.Instance.DraftingHidage))
                            {
                                result.AddFactor(0.5f, DefaultDemesneLaws.Instance.DraftingHidage.Name);
                            }
                        }
                    }

                    var education = BannerKingsConfig.Instance.EducationManager.GetHeroEducation(buyerHero);
                    if (troop.Occupation == Occupation.Mercenary && education.HasPerk(BKPerks.Instance.MercenaryLocalConnections))
                    {
                        result.AddFactor(-0.1f, BKPerks.Instance.MercenaryLocalConnections.Name);
                    }

                    if (troop.IsMounted && education.HasPerk(BKPerks.Instance.RitterOathbound))
                    {
                        result.AddFactor(-0.15f, BKPerks.Instance.RitterOathbound.Name);
                    }

                    if (Utils.Helpers.IsRetinueTroop(troop))
                    {
                        result.AddFactor(0.20f);
                    }

                    if (troop.Culture == buyerHero.Culture)
                    {
                        result.AddFactor(-0.05f, GameTexts.FindText("str_culture"));
                    }

                    if (education.Lifestyle != null && education.Lifestyle.Equals(DefaultLifestyles.Instance.Artisan))
                    {
                        result.AddFactor(0.15f, DefaultLifestyles.Instance.Artisan.Name);
                    }

                    if (buyerHero.Clan != null)
                    {
                        if (troop.Culture.StringId == "aserai" && BannerKingsConfig.Instance.ReligionsManager
                            .HasBlessing(buyerHero, DefaultDivinities.Instance.AseraSecondary2))
                        {
                            result.AddFactor(-0.1f);
                        }

                        var buyerKingdom = buyerHero.Clan.Kingdom;
                        if (buyerKingdom != null && troop.Culture != buyerHero.Culture)
                        {
                            result.AddFactor(0.25f, GameTexts.FindText("str_kingdom"));
                        }

                        switch (buyerHero.Clan.Tier)
                        {
                            case >= 4:
                                result.AddFactor((buyerHero.Clan.Tier - 3) * 0.05f);
                                break;
                            case <= 1:
                                result.AddFactor((buyerHero.Clan.Tier - 2) * 0.05f);
                                break;
                        }
                    }
                }
            },
            GetType().Name,
            false);
            
            return (int) result.ResultNumber;
        }
    }
}