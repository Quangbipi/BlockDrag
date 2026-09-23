using Base;
using DG.Tweening;
using Solo.MOST_IN_ONE;
using UnityEngine;

namespace UI
{
    public static class UI_ANIM 
    {
        public static void Breaking(BreakingUI unit, Vector3 rotation = default, float time = 0.3f)
        {
            unit.CurrentAnim.Kill();
            unit.CurrentAnim = DOTween.Sequence();
            // unit.SkinTf.localPosition = new Vector3(unit.SkinTf.localPosition.x, startHeight, unit.SkinTf.localPosition.z);
            unit.CurrentAnim.Append(unit.SkinTf.DOLocalRotate(rotation, time * 0.4f));
            unit.CurrentAnim.Append(unit.SkinTf.DOShakePosition(time * 0.3f, 12, 100, 90, fadeOut: false, randomnessMode: ShakeRandomnessMode.Harmonic)
            .OnComplete(() => {
                unit.Parts[0].gameObject.SetActive(false);
                unit.Parts[1].gameObject.SetActive(true);
                unit.Parts[2].gameObject.SetActive(true);
                unit.BreakingVfx?.Play();
                Locator.Audio.PlaySfx(SFX_TYPE.HIT);
                MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.HeavyImpact, 0.1f);
                }));
            unit.CurrentAnim.Append(unit.SkinTf.DOShakePosition(time * 0.3f, 12, 100, 90, randomnessMode: ShakeRandomnessMode.Harmonic));
            unit.CurrentAnim.Join(unit.Parts[1].transform.DOLocalMoveX(unit.Parts[1].transform.localPosition.x - 30, time * 0.3f));
            unit.CurrentAnim.Join(unit.Parts[2].transform.DOLocalMoveX(unit.Parts[2].transform.localPosition.x + 30, time * 0.3f));
            unit.CurrentAnim.AppendInterval(time * 0.25f);
            unit.CurrentAnim.Join(unit.Parts[1].DOFade(0, time * 0.25f));
            unit.CurrentAnim.Join(unit.Parts[2].DOFade(0, time * 0.25f));
            unit.CurrentAnim.Play();
        }
    }
}
