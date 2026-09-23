using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public interface IAdsService
    {
        IAds AppOpen { get; }
        IAds Banner { get; }
        IRewardAds Reward { get; }
        IInterAds Inter { get; }
        IAnalytic Analytic { get; }
        public ADS_TYPE Type { get; set; }
    }

    public interface IAds
    {
        public void Show();
        public void Hide();
        public void Load();
        public ADS_TYPE Type { get; set; }
    }
    public interface IRewardAds : IAds
    {
        public Action _OnTriggerLoadAds { get; }
        public Action<Action> _OnAddLoadAds { get; }
        public void Show(Action rewardCallBack, Action hiddenCallBack = null, Placement placement = Placement.NONE);
    }
    public interface IInterAds : IAds
    {
        public Action _OnTriggerLoadAds { get; }
        public Action<Action> _OnAddLoadAds { get; }
        public void Show(Action callback);
    }
    public interface IBannerAds : IAds
    {
        public void Show(ADS_TYPE type);
        public void Hide(ADS_TYPE type);
        public void Init();
    }
    public interface IAnalytic
    {
        public abstract void AdsRewardOffer(Placement place);
        public abstract void AdsRewardClick(Placement place);
        public abstract void AdsRewardShow(Placement place);
        public abstract void AdsRewardShowFail(Placement place, string error);
        public abstract void AdsRewardComplete(Placement place, string type);
        public abstract void AdsRewardLoadComplete();
        public abstract void AdsRewardLoad();

        public abstract void AdsInterFail(string error);
        public abstract void AdsInterLoad();
        public abstract void AdsInterShow(Placement place);
        public abstract void AdsInterClick();
        public abstract void AdsInterLoadComplete();
        public abstract void AdsInterComplete();
        public abstract void TutorialStep(string name, int step);


        //public void ResourceSpend(BoosterType type, Resource.Placement place, int amount)
        //{
        //    if (!isFirebaseInit) return;

        //    FirebaseAnalytics.LogEvent("resource_spend", new Parameter[]
        //    {
        //        new Parameter("name", type.ToString()),
        //        new Parameter("placement", place.ToString()),
        //        new Parameter("value", amount)
        //    });
        //}

        public abstract void FireUserProps();

        public abstract void Day();
        public abstract void GoogleFireBaseTrackEvent(string name);

        public abstract void AppsFlyerTrackParamEvent(string name, Dictionary<string, string> param);

        public abstract void LevelTrackEvent(LEVEL_STATE state, int value = 0);
        public abstract void EarnVirtualCurrency(string name, long value, string source);
        public abstract void SpendVirtualCurrency(string name, long value, string itemName);
    }
}
