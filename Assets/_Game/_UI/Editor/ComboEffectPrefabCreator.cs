#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    public static class ComboEffectPrefabCreator
    {
        private const string PrefabDir = "Assets/_Game/Resources/Prefabs";
        private const string PrefabPath = "Assets/_Game/Resources/Prefabs/ComboTextEffect.prefab";
        private const string FontGuid = "09e79763ca66e0d439a350079365adb2"; // Fredoka-Bold SDF

        [MenuItem("Tools/BlockDrag/Create Combo Effect UI Prefab")]
        public static GameObject CreateOrUpdatePrefab()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
                AssetDatabase.Refresh();
            }

            string fontPath = AssetDatabase.GUIDToAssetPath(FontGuid);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

            // Tạo Root UI GameObject
            GameObject rootGo = new GameObject("ComboTextEffect", typeof(RectTransform), typeof(CanvasGroup), typeof(ComboTextEffect));
            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(400, 100);
            CanvasGroup cvg = rootGo.GetComponent<CanvasGroup>();
            ComboTextEffect effectComp = rootGo.GetComponent<ComboTextEffect>();

            // Tạo Sub-Object ComboLabel (UI)
            GameObject labelGo = new GameObject("ComboLabel", typeof(RectTransform));
            labelGo.transform.SetParent(rootGo.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(1f, 0.5f);
            labelRect.anchoredPosition = new Vector2(-10f, 0f);
            labelRect.sizeDelta = new Vector2(250f, 80f);

            TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null)
            {
                labelTmp.font = fontAsset;
            }
            labelTmp.text = "COMBO";
            labelTmp.fontSize = 50f;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.Right;
            labelTmp.color = new Color(1f, 0.82f, 0.15f); // Vàng tươi

            // Tạo Sub-Object ComboNumber (UI)
            GameObject numberGo = new GameObject("ComboNumber", typeof(RectTransform));
            numberGo.transform.SetParent(rootGo.transform, false);
            RectTransform numberRect = numberGo.GetComponent<RectTransform>();
            numberRect.anchorMin = new Vector2(0.5f, 0.5f);
            numberRect.anchorMax = new Vector2(0.5f, 0.5f);
            numberRect.pivot = new Vector2(0f, 0.5f);
            numberRect.anchoredPosition = new Vector2(10f, 0f);
            numberRect.sizeDelta = new Vector2(150f, 80f);

            TextMeshProUGUI numberTmp = numberGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null)
            {
                numberTmp.font = fontAsset;
            }
            numberTmp.text = "x2";
            numberTmp.fontSize = 58f;
            numberTmp.fontStyle = FontStyles.Bold;
            numberTmp.alignment = TextAlignmentOptions.Left;
            numberTmp.color = new Color(1f, 1f, 0.9f); // Trắng sáng

            // Gán references vào component bằng SerializedObject
            SerializedObject so = new SerializedObject(effectComp);
            so.FindProperty("comboLabelText").objectReferenceValue = labelTmp;
            so.FindProperty("comboNumberText").objectReferenceValue = numberTmp;
            so.FindProperty("labelContainer").objectReferenceValue = labelGo.transform;
            so.FindProperty("numberContainer").objectReferenceValue = numberGo.transform;
            so.FindProperty("canvasGroup").objectReferenceValue = cvg;
            so.FindProperty("rectTransform").objectReferenceValue = rootRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Lưu Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ComboEffectPrefabCreator] Created UI ComboTextEffect prefab at: {PrefabPath}");
            return savedPrefab;
        }
    }
}
#endif
