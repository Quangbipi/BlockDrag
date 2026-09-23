using System.Collections.Generic;
using Base;
using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIHomeItemData
    {
        public int Level;
        public int CurrentStar;
        public int Type;
        public int RequireStar;
    }
    public class UIHomeLevelItem : MonoBehaviour, ICell
    {
        [SerializeField]
        protected GameObject activeObj;
        [SerializeField]
        protected GameObject deactiveObj;
        [SerializeField]
        protected GameObject lockObj;
        [SerializeField]
        protected TMP_Text levelTxt;
        [SerializeField]
        protected TMP_Text levelDeactiveTxt;
        [SerializeField]
        protected GameObject requireStarObj;
        [SerializeField]
        protected TMP_Text requireStarTxt;
        [SerializeField]
        protected List<GameObject> connects;
        [SerializeField]
        protected List<Image> stars;
        [SerializeField]
        protected List<Sprite> starSprites;
        [SerializeField]
        protected Image selectingImage;
        protected int _cellIndex;
        public int CellIndex => _cellIndex;

        public void ConfigureCell(UIHomeItemData data, int currentTotalStar, int levelPerChapter, int cellIndex)
        {
            _cellIndex = cellIndex;
            levelTxt.text = $"{data.Level + 1}";
            levelDeactiveTxt.text = $"{data.Level + 1}";
            if (data.Level == 0)
            {
                connects[0].gameObject.SetActive(false);
                connects[1].gameObject.SetActive(true);
            }
            else if (data.Level == CONSTANTS.MAX_LEVEL)
            {
                connects[0].gameObject.SetActive(true);
                connects[1].gameObject.SetActive(false);
            }
            else
            {
                connects[0].gameObject.SetActive(true);
                connects[1].gameObject.SetActive(true);
            }

            if (currentTotalStar >= data.RequireStar)
            {
                deactiveObj.SetActive(false);

                for (int i = 0; i < stars.Count; i++)
                {
                    if (i < data.CurrentStar)
                    {
                        stars[i].sprite = starSprites[1];
                    }
                    else
                    {
                        stars[i].sprite = starSprites[0];
                    }
                }
            }
            else
            {
                deactiveObj.SetActive(true);
                if (data.Level == CONSTANTS.MAX_LEVEL)
                {
                    lockObj.gameObject.SetActive(true);
                    requireStarObj.gameObject.SetActive(false);
                    requireStarTxt.text = $"Comming Soon!!";
                }
                else if (data.Level % levelPerChapter == 0)
                {
                    lockObj.gameObject.SetActive(false);
                    requireStarObj.gameObject.SetActive(true);
                    requireStarTxt.text = $"{data.RequireStar - currentTotalStar} more";
                }
                else
                {
                    lockObj.gameObject.SetActive(false);
                    requireStarObj.gameObject.SetActive(false);
                }
            }

        }

        public void Select(bool value)
        {
            if (value)
            {
                selectingImage.gameObject.SetActive(true);
            }
            else
            {
                selectingImage.gameObject.SetActive(false);
            }
        }
    }
}
