using System.Collections;
using System.Collections.Generic;


namespace Base.UI
{
#if UNITY_EDITOR
    using Sirenix.OdinInspector;
    using UnityEditor.U2D;
#endif
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.U2D;
    using UnityEngine.UI;
    public class UISCanvas : UICanvas
    {
        public Action<UISCanvas> _OnOpen;
        public Action<UISCanvas> _OnClose;

        [Serializable]
        protected class Propertys
        {
            public bool SetActiveByAnim = true;
        }

        [SerializeField]
        protected UIAnim[] anims;
        [SerializeField]
        protected Button closeButton;
        [SerializeField]
        protected Propertys data;
        [SerializeField]
        protected UICanvasComponent[] canvasComponent;
        [SerializeField]
        protected SpriteAtlas spriteAtlas;

        protected int longestHideAnimId = 0;
        protected float longestHideAnimTime = 0;

        protected virtual void Start()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i]._OnAnimEnter += OnAnimEnter;
                anims[i]._OnAnimExit += OnAnimExit;
                for (int j = 0; j < anims[i].Datas.Count; j++)
                {
                    UIAnim.Propertys Data = anims[i].Datas[j];
                    if (Data != null && Data.Id == UIAnim.ANIM.HIDE)
                    {
                        if (Data.Time > longestHideAnimTime)
                        {
                            longestHideAnimTime = Data.Time;
                            longestHideAnimId = anims[i].GetHashCode();
                        }
                        break;
                    }
                }
            }
            closeButton?.onClick.AddListener(Close);
        }

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i]._OnAnimEnter -= OnAnimEnter;
                anims[i]._OnAnimExit -= OnAnimExit;
            }
            closeButton?.onClick.RemoveListener(Close);
        }

        public override void Open(object param)
        {
            base.Open(param);
            Show();
            _OnOpen?.Invoke(this);
        }
        public override void Show()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].Play(UIAnim.ANIM.SHOW);
            }
            for (int i = 0; i < canvasComponent.Length; i++)
            {
                canvasComponent[i].Show();
            }
        }

        public override void Hide()
        {
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].Play(UIAnim.ANIM.HIDE);
            }
            for (int i = 0; i < canvasComponent.Length; i++)
            {
                canvasComponent[i].Hide();
            }
        }
        public override void Close()
        {
            _OnClose?.Invoke(this);
            if (anims.Length > 0)
            {
                Hide();
            }
            else
            {
                base.Close();
            }
        }
        protected virtual void OnAnimEnter(int id, int anim)
        {
            if (data.SetActiveByAnim)
            {
                switch (anim)
                {
                    case (int)UIAnim.ANIM.SHOW:
                        if (!gameObject.activeInHierarchy)
                        {
                            gameObject.SetActive(true);
                        }
                        break;
                }

            }

        }
        protected virtual void OnAnimExit(int id, int anim)
        {
            if (data.SetActiveByAnim)
            {
                switch (anim)
                {
                    case (int)UIAnim.ANIM.HIDE:
                        if (id == longestHideAnimId)
                        {
                            base.Close();
                        }
                        break;
                }
            }
        }
        public virtual T GetCanvasComponent<T>() where T : UICanvasComponent
        {
            for (int i = 0; i < canvasComponent.Length; i++)
            {
                if (canvasComponent[i] is T)
                {
                    return (T)canvasComponent[i];
                }
            }
            return null;
        }
#if UNITY_EDITOR
        [Button(ButtonSizes.Large), HorizontalGroup("Func", 0.3f)]
        public virtual void AddAtlasSprite()
        {
            if (spriteAtlas != null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image image in images)
                {
                    if (image.sprite == null) continue;
                    AtlasSprite atlasSprite = image.gameObject.GetComponent<AtlasSprite>();
                    if (atlasSprite == null)
                        atlasSprite = image.gameObject.AddComponent<AtlasSprite>();
                    atlasSprite.atlas = spriteAtlas;
                }
            }
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Func", 0.4f)]
        public virtual void UpdateAtlasSpriteData()
        {
            List<Sprite> packedSprites = new List<Sprite>();

            UnityEngine.Object[] objects = spriteAtlas.GetPackables();
            for (int i = 0; i < objects.Length; i++)
            {
                packedSprites.Add(objects[i] as Sprite);
            }

            List<Sprite> addSprite = new List<Sprite>();
            if (spriteAtlas != null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image image in images)
                {
                    if (image.sprite == null) continue;
                    if (!packedSprites.Contains(image.sprite))
                    {
                        packedSprites.Add(image.sprite);
                        addSprite.Add(image.sprite);
                    }
                }
            }

            if (addSprite.Count == 0) return;
            SpriteAtlasAsset atlasAsset = SpriteAtlasAsset.Load(AssetDatabase.GetAssetPath(spriteAtlas));
            List<UnityEngine.Object> save = new List<UnityEngine.Object>();
            for (int i = 0; i < addSprite.Count; i++)
            {
                save.Add(addSprite[i]);
            }

            atlasAsset.Add(save.ToArray());
            SpriteAtlasAsset.Save(atlasAsset, AssetDatabase.GetAssetPath(spriteAtlas));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Func", 0.3f)]
        public virtual void GetAllAnim()
        {
            anims = GetComponentsInChildren<UIAnim>(true);
        }
#endif

    }
}
