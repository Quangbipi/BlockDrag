using System.Collections.Generic;
using Base.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ListItemShow : MonoBehaviour
    {
        [SerializeField]
        protected List<Image> images;
        [SerializeField]
        protected List<BreakingUI> breakingImages;
        [SerializeField]
        protected List<ParticleSystem> addParticles;
        [SerializeField]
        protected List<ParticleSystem> removeParticles;
        [SerializeField]
        protected List<UIAnim> anims;
        [SerializeField]
        protected List<Sprite> states;

        protected int currentIndex = 0;
        protected int max;
        public List<Image> Images => images;
        public List<BreakingUI> BreakingImages => breakingImages;
        void Awake()
        {
            max = images.Count;
        }

        public void ResetAll()
        {
            for (int i = 0; i < max; i++)
            {
                if (i < images.Count)
                {
                    images[i].gameObject.SetActive(true);
                    images[i].sprite = states[0];
                }

                if (i < breakingImages.Count)
                {
                    breakingImages[i].Reset();
                }
            }
            currentIndex = -1;
        }
        public void SetMaxItems(int max)
        {
            this.max = max;
            for (int i = 0; i < images.Count; i++)
            {
                if (i < max)
                {
                    images[i].gameObject.SetActive(true);
                }
                else
                {
                    images[i].gameObject.SetActive(false);
                }
            }
            currentIndex = -1;
        }
        public void ActiveItems(int index, int state)
        {
            int value = index - (currentIndex + 1);
            if (value == 0) return;

            for (int i = 0; i < Mathf.Abs(value); i++)
            {
                if (Mathf.Sign(value) > 0)
                {
                    Add();
                }
                else
                {
                    Remove();
                }
            }
        }
        public void Add()
        {
            currentIndex += 1;
            if (currentIndex >= max)
            {
                currentIndex = Mathf.Clamp(max - 1, 0, max);
            }

            if (currentIndex < addParticles.Count)
            {
                addParticles[currentIndex].Play();
            }
            if (currentIndex < images.Count)
            {
                images[currentIndex].sprite = states[1];
            }
        }
        public void Remove()
        {
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            
            if (currentIndex < images.Count)
            {
                images[currentIndex].sprite = states[0];
            }

            if (currentIndex < removeParticles.Count)
            {
                removeParticles[currentIndex].Play();
            }
            if (breakingImages != null && currentIndex < breakingImages.Count)
            {
                breakingImages[currentIndex].Breaking();
            }
            currentIndex -= 1;
            if (currentIndex < -1)
            {
                currentIndex = -1;
            }
        }

        public void PlayItemAnim(UIAnim.ANIM anim)
        {
            if (currentIndex < anims.Count)
            {
                anims[currentIndex].Play(anim);
            }
        }
    }
}
