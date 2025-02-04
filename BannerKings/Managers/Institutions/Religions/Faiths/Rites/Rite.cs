﻿using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Institutions.Religions.Faiths.Rites
{
    public abstract class Rite
    {
        public abstract TextObject GetName();
        public abstract TextObject GetDescription();
        public abstract RiteType GetRiteType();
        public abstract float GetTimeInterval(Hero hero);

        public abstract void Execute(Hero executor);
        public abstract void SetDialogue();
        public abstract float GetPietyReward();
        public abstract bool MeetsCondition(Hero hero);
        public abstract void Complete(Hero actionTaker);
        public abstract TextObject GetRequirementsText(Hero hero);
    }

    public enum RiteType
    {
        OFFERING,
        SACRIFICE,
        DONATION
    }
}