using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class BreakingUI : MonoBehaviour
    {
        [SerializeField]
        Transform tf;
        [SerializeField]
        Transform skinTf;
        [SerializeField]
        List<Image> parts;
        [SerializeField]
        ParticleSystem breakingVfx;
        protected List<Vector3> originLocalPartPositions;
        protected List<Vector3> originLocalPartRotations;
        public Vector3 OriginAnimPos { get; set; }
        public Vector3 OriginAnimRotation { get; set; }
        public Vector3 OriginAnimScale { get; set; }
        public Transform Tf => tf;
        public Transform SkinTf => skinTf;
        public Sequence CurrentAnim { get; set; }
        public List<Image> Parts => parts;
        public ParticleSystem BreakingVfx => breakingVfx;

        protected void Awake()
        {
            originLocalPartPositions = new List<Vector3>();
            originLocalPartRotations = new List<Vector3>();
            OriginAnimRotation = SkinTf.localEulerAngles;
            for (int i = 0; i < parts.Count; i++)
            {
                originLocalPartPositions.Add(parts[i].transform.localPosition);
                originLocalPartRotations.Add(parts[i].transform.localEulerAngles);
            }
        }
        public void Reset()
        {
            for (int i = 0; i < parts.Count; i++)
            {
                Color color = parts[i].color;
                color.a = 1;
                parts[i].color = color;
                parts[i].transform.localPosition = originLocalPartPositions[i];
                parts[i].transform.localEulerAngles = originLocalPartRotations[i];
                parts[i].gameObject.SetActive(false);
            }
            SkinTf.localEulerAngles = OriginAnimRotation;
            parts[0].gameObject.SetActive(true);
        }

        [Button]
        public void Breaking()
        {
            Reset();
            UI_ANIM.Breaking(this, Vector3.forward * 25, 0.6f);
        }
    }
}
