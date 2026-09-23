using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public static class Locator
    {
        private static IUIService ui;
        public static IUIService UI
        {
            get => ui;
            set
            {
                ui = value;
            }
        }
        private static IPiggyBankService piggyBank;
        public static IPiggyBankService PiggyBank
        {
            get => piggyBank;
            set
            {
                piggyBank = value;
            }
        }
        private static ISpinWheelService spinWheel;
        public static ISpinWheelService SpinWheel
        {
            get => spinWheel;
            set
            {
                spinWheel = value;
            }
        }
        private static IDailyRewardService dailyReward;
        public static IDailyRewardService DailyReward
        {
            get => dailyReward;
            set
            {
                dailyReward = value;
            }
        }
        private static IDailyGiftService dailyGift;
        public static IDailyGiftService DailyGift
        {
            get => dailyGift;
            set
            {
                dailyGift = value;
            }
        }
        private static IDataService data;
        public static IDataService Data
        {
            get => data;
            set
            {
                data = value;
            }
        }

        private static IAdsService ads;
        public static IAdsService Ads
        {
            get => ads;
            set
            {
                ads = value;
            }
        }
        private static IAudioService audio;
        public static IAudioService Audio
        {
            get => audio;
            set
            {
                audio = value;
            }
        }

        private static ILevelService level;
        public static ILevelService Level
        {
            get => level;
            set
            {
                level = value;
            }
        }

        private static IGameplayService gameplay;
        public static IGameplayService Gameplay
        {
            get => gameplay;
            set
            {
                gameplay = value;
            }
        }
        private static IHeartService heart;
        public static IHeartService Heart
        {
            get => heart;
            set
            {
                heart = value;
            }
        }
        private static ITutorialService tutorial;
        public static ITutorialService Tutorial
        {
            get => tutorial;
            set
            {
                tutorial = value;
            }
        }
        private static IIAPService iAPService;
        public static IIAPService IAPService
        {
            get => iAPService;
            set
            {
                iAPService = value;
            }
        }
        private static IItemService reward;
        public static IItemService Reward
        {
            get => reward;
            set
            {
                reward = value;
            }
        }
    }
}