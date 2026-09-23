using System;
using System.Collections;
using System.Collections.Generic;
using Base.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Utilities.Timer;

namespace UI
{
    public class UIPositionAnim : UIAnim
    {
        [Serializable]
        private new class Propertys : UIAnim.Propertys
        {
            public Transform StartTf;
            public Transform EndTf;
            public bool IsSetPositionToStart = true;
            [HideInInspector]
            public Vector3 OriginPos;
            public bool IsReturnOriginPos = false;
        }
        [SerializeField]
        Transform tf;
        [SerializeField]
        Propertys[] datas;
        Tween currentAnim;
        public override IReadOnlyList<UIAnim.Propertys> Datas => datas;

        public override void Stop()
        {
            tf.DOKill();
        }
        public override void Play(ANIM anim)
        {
            if (state != ANIM.NONE) return;
            Propertys Data = Array.Find(datas, data => data.Id == anim);
            if (Data == null) return;

            Data.OriginPos = tf.position;
            if (Data.IsSetPositionToStart)
            {
                tf.position = Data.StartTf.position;
            }
            state = anim;
            TimerManager.Ins.WaitForFrame(5, () =>
            {
                switch (anim)
                {
                    case ANIM.SHOW:
                        OnAnimEnter((int)ANIM.SHOW);
                        currentAnim?.Kill();
                        currentAnim = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease).OnComplete(
                            () =>
                            {
                                OnAnimExit((int)ANIM.SHOW);
                                state = ANIM.NONE;
                                if (Data.IsReturnOriginPos)
                                {
                                    transform.position = Data.OriginPos;
                                }
                            });
                        break;
                    case ANIM.HIDE:
                        OnAnimEnter((int)ANIM.HIDE);
                        currentAnim?.Kill();
                        currentAnim = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease).OnComplete(() =>
                        {
                            OnAnimExit((int)ANIM.HIDE);
                            state = ANIM.NONE;
                            if (Data.IsReturnOriginPos)
                            {
                                transform.position = Data.OriginPos;
                            }
                        });
                        break;
                    case ANIM.IDLE:
                        OnAnimEnter((int)ANIM.IDLE);
                        currentAnim?.Kill();
                        currentAnim = tf.DOMove(Data.EndTf.position, Data.Time).SetEase(Data.Ease)
                        .SetLoops(2, LoopType.Yoyo).OnComplete(() =>
                        {
                            OnAnimExit((int)ANIM.IDLE);
                            state = ANIM.NONE;
                            if (Data.IsReturnOriginPos)
                            {
                                transform.position = Data.OriginPos;
                            }
                        });
                        break;
                }
            });

        }
        [Button]
        public override void SetupBaseData()
        {
            tf ??= GetComponent<Transform>();
            tf = tf.Equals(null) ? GetComponent<Transform>() : tf;
            if (datas.Length < 2)
            {
                datas = new Propertys[2];
            }
            Propertys Data = Array.Find(datas, data => data?.Id == ANIM.SHOW);
            if (Data == null)
            {
                datas[0] = new Propertys()
                {
                    Id = ANIM.SHOW,
                    Time = 0.3f,
                    Ease = Ease.OutBack,
                    // StartTf = tf.parent?.GetChild(2),
                    // EndTf = tf.parent?.GetChild(1),
                };
            }
            else
            {
                datas[0].Id = ANIM.SHOW;
                datas[0].Time = 0.3f;
                datas[0].Ease = Ease.OutBack;
                // datas[0].StartTf = tf.parent?.GetChild(2);
                // datas[0].EndTf = tf.parent?.GetChild(1);
            }

            Propertys Data2 = Array.Find(datas, data => data?.Id == ANIM.HIDE);
            if (Data2 == null)
            {
                datas[1] = new Propertys()
                {
                    Id = ANIM.HIDE,
                    Time = 0.3f,
                    Ease = Ease.InBack,
                    // StartTf = tf.parent?.GetChild(1),
                    // EndTf = tf.parent?.GetChild(2),
                };
            }
            else
            {
                datas[1].Id = ANIM.HIDE;
                datas[1].Time = 0.3f;
                datas[1].Ease = Ease.InBack;
                // datas[1].StartTf = tf.parent?.GetChild(1);
                // datas[1].EndTf = tf.parent?.GetChild(2);
            }
        }
    }
}
