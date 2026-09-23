using System;
using System.Collections.Generic;
using Base;
using MEC;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI
{
    using Utilities;
    public class UIFlyReward : MonoBehaviour
    {
        [FoldoutGroup("References")] [SerializeField]
        private Image image;

        [FoldoutGroup("References")] [SerializeField]
        private Sprite icon;

        [FoldoutGroup("References")] [SerializeField]
        private TextMeshProUGUI txtAmount;

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private Image rewardPrefab;

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private RectTransform rewardFlyContainer;

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private float amountToNumRewardFlyRatio = 1f / 10;

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private float delayBetweenSpawnRewardFly = 0.2f;

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private float flyDuration = 0.5f;

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private AnimationCurve easeFly = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private AnimationCurve easePerpendicularFly = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [FoldoutGroup("Reward Fly")] [SerializeField]
        private AnimationCurve easeScaleFly = AnimationCurve.Constant(0, 1, 1);

        public void InitReward(int start, int end, Vector3 goldStartPos, Vector3 goldEndPos = default)
        {
            image.sprite = icon;
            image.color = Color.white;
            txtAmount.text = start.ToString();
            if (goldEndPos == default) goldEndPos = rewardFlyContainer.position;
            FlyReward(start, end, goldStartPos, goldEndPos);
        }

        private void FlyReward(int start, int end, Vector3 startPos, Vector3 endPos)
        {
            int amount = end - start;
            if (amount <= 0) return;

            int numObjectFly = Mathf.CeilToInt(amount * amountToNumRewardFlyRatio);

            for (int i = 0; i < numObjectFly; i++)
            {
                var reward = Instantiate(rewardPrefab, startPos, Quaternion.identity, rewardFlyContainer);
                reward.sprite = icon;
                reward.transform.localScale = Vector3.zero;
                reward.transform.localRotation = Quaternion.identity;
                SpawnGoldFlyVector3(reward.transform, startPos, endPos, i * delayBetweenSpawnRewardFly, GetNewAmount(i), OnDoneGoldFly);
            }

            return;

            void OnDoneGoldFly(Transform reward, int newAmount)
            {
                Locator.Audio.PlaySfx(SFX_TYPE.COIN);
                txtAmount.text = newAmount.ToString();
                Destroy(reward.gameObject, 0.05f);
            }
            
            int GetNewAmount(int i)
            {
                if(i == numObjectFly - 1)
                {
                    return end;
                }
                return Mathf.CeilToInt(start + (i + 1) / amountToNumRewardFlyRatio);
            }
        }

        private void SpawnGoldFlyVector3(Transform reward, Vector3 startPos, Vector3 endPos, float delay, int newAmount,
            Action<Transform, int> onCompleted)
        {
            Timing.RunCoroutine(IEGoldFly(delay));
            return;

            IEnumerator<float> IEGoldFly(float d)
            {
                yield return Timing.WaitForSeconds(d);
                float time = 0;
                Vector3 directionNorm = (endPos - startPos).normalized;
                float distance = Vector3.Distance(startPos, endPos);
                var perpendicularDirection = new Vector3(-directionNorm.y, directionNorm.x);
                float randomDistance = Random.Range(-distance / 2, distance / 2);
                while (time < flyDuration)
                {
                    time += Time.unscaledDeltaTime;
                    float t = time / flyDuration;
                    reward.position = Vector3.Lerp(startPos, endPos, easeFly.Evaluate(t))
                                      + perpendicularDirection *
                                      (randomDistance * easePerpendicularFly.Evaluate(t));
                    reward.localScale = Vector3.one * easeScaleFly.Evaluate(t);
                    yield return Timing.WaitForOneFrame;
                }

                onCompleted?.Invoke(reward, newAmount);
            }
        }
        
        private void SpawnGoldFly(Transform reward, Vector3 startPos, Vector3 endPos, float delay, int newAmount,
            Action<Transform, int> onCompleted)
        {
            Timing.RunCoroutine(IEGoldFly(delay));
            return;

            IEnumerator<float> IEGoldFly(float d)
            {
                yield return Timing.WaitForSeconds(d);
                float time = 0;
                Vector2 directionNorm = (endPos - startPos).normalized;
                float distance = Vector2.Distance(startPos, endPos);
                var perpendicularDirection = new Vector2(-directionNorm.y, directionNorm.x);
                float randomDistance = Random.Range(-distance / 2, distance / 2);
                while (time < flyDuration)
                {
                    time += Time.unscaledDeltaTime;
                    float t = time / flyDuration;
                    reward.position = Vector2.Lerp(startPos, endPos, easeFly.Evaluate(t))
                                      + perpendicularDirection *
                                      (randomDistance * easePerpendicularFly.Evaluate(t));
                    reward.localScale = Vector3.one * easeScaleFly.Evaluate(t);
                    yield return Timing.WaitForOneFrame;
                }

                onCompleted?.Invoke(reward, newAmount);
            }
        }
    }
}