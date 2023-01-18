﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BannerKings.Actions
{
    public static class ClanActions
    {
        public static TextObject CanCreateNewClan(CultureObject culture, Settlement settlement, TextObject name = null)
        {
            if (name == null)
            {
                name = GetRandomName(culture, settlement);
            }

            var names = new List<string>();
            foreach (var existingClan in Clan.All.ToList().FindAll(x => x.Culture == culture))
            {
                names.Add(existingClan.Name.ToString());
            }

            if (name == null || names.Any(x => x.Contains(name.ToString()) || x.ToString().Equals(name.ToString())))
            {
                return null;
            }

            return name;
        }

        public static Clan CreateNewClan(Hero hero, Settlement settlement, string id, TextObject name = null,
            float renown = 150f, bool removeGold = false)
        {
            if (name == null)
            {
                name = CanCreateNewClan(hero.Culture, settlement);
            }

            if (name == null)
            {
                return null;
            }

            var originalClan = hero.Clan;
            var clan = Clan.CreateClan(id);

            hero.Clan = null;
            hero.CompanionOf = null;
            clan.InitializeClan(name, name, hero.Culture, Banner.CreateOneColoredBannerWithOneIcon(
                settlement.MapFaction.Banner.GetFirstIconColor(), settlement.MapFaction.Banner.GetPrimaryColor(),
                hero.Culture.PossibleClanBannerIconsIDs.GetRandomElement()), settlement.GatePosition);
            clan.AddRenown(renown);
            hero.Clan = clan;
            clan.SetLeader(hero);
            clan.UpdateHomeSettlement(settlement);
            if (hero.Spouse != null && !Utils.Helpers.IsClanLeader(hero.Spouse))
            {
                JoinClan(hero.Spouse, clan);
            }

            if (hero.Children.Count > 0)
            {
                foreach (var child in hero.Children)
                {
                    if (child.IsChild)
                    {
                        JoinClan(child, clan);
                    }
                }
            }

            if (originalClan != null)
            {
                ChangeKingdomAction.ApplyByJoinToKingdom(clan, originalClan.Kingdom, false);
            }

            BannerKingsConfig.Instance.TitleManager.RemoveKnights(hero);
            if (removeGold)
            {
                hero.ChangeHeroGold(-50000);
            }

            return clan;
        }

        public static void JoinClan(Hero hero, Clan clan)
        {
            hero.Clan = null;
            hero.CompanionOf = null;
            hero.SetNewOccupation(Occupation.Lord);
            hero.Clan = clan;
        }

        public static TextObject GetRandomName(CultureObject culture, Settlement settlement)
        {
            TextObject random;
            if (culture.ClanNameList.Count > 1)
            {
                random = culture.ClanNameList.GetRandomElement();
            }
            else
            {
                random = culture.ClanNameList[0];
                random.SetTextVariable("ORIGIN_SETTLEMENT", settlement.Name);
            }

            return random;
        }
    }
}