using Quang.Ads;
using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag.Ads
{
    /// <summary>
    /// Registers a no-op ads service until a real mediation SDK (AdsManager) is added.
    /// A real AdsManager assigns Locator.Ads in its Awake, which runs after this, and replaces it.
    /// </summary>
    public static class AdsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Install();
        }

        public static void Install()
        {
            if (Locator.Ads == null)
            {
                Locator.Ads = new NullAdsService();
            }
        }
    }
}
