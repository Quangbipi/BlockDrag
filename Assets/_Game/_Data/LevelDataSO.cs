using Base;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataSO", menuName = "ScriptableObjects/LevelDataSO")]
public class LevelDataSO : SerializedScriptableObject
{
    //[SerializeField]
    //Dictionary<int, List<Seed>> staffSeeds;
    //[SerializeField]
    //Dictionary<int, List<List<StaffColorProperty>>> staffColorPropertys;
    //[SerializeField]
    //StaffSeedSO staffSeed;
    [SerializeField]
    [TableList(ShowIndexLabels = true, DefaultMinColumnWidth = 40)]
    List<Data> detailData;
    public List<Data> DetailData => detailData;

#if UNITY_EDITOR
    #region Helper

    [Button(ButtonSizes.Large, ButtonAlignment = 0.5f), HorizontalGroup("Init", 0.2f)]
    public void AdjustData()
    {
        for (int i = 0; i < detailData.Count; i++)
        {
            for (int j = 0; j < detailData[i].LevelData.knifeData.Count; j++)
            {
                int x = Mathf.RoundToInt(detailData[i].LevelData.knifeData[j].position.x / 2);
                int z = Mathf.RoundToInt(detailData[i].LevelData.knifeData[j].position.z / 2);
                Vector3 newPos = new Vector3(x * 2, detailData[i].LevelData.knifeData[j].position.y, z * 2);
                detailData[i].LevelData.knifeData[j].position = newPos;
            }

            for (int j = 0; j < detailData[i].LevelData.riceData.Count; j++)
            {
                int x = Mathf.RoundToInt(detailData[i].LevelData.riceData[j].position.x / 2);
                int z = Mathf.RoundToInt(detailData[i].LevelData.riceData[j].position.z / 2);
                Vector3 newPos = new Vector3(x * 2, detailData[i].LevelData.riceData[j].position.y, z * 2);
                detailData[i].LevelData.riceData[j].position = newPos;
            }

            for (int j = 0; j < detailData[i].LevelData.holderSlotData.Count; j++)
            {
                int x = Mathf.RoundToInt(detailData[i].LevelData.holderSlotData[j].position.x / 2);
                int z = Mathf.RoundToInt(detailData[i].LevelData.holderSlotData[j].position.z / 2);
                Vector3 newPos = new Vector3(x * 2, detailData[i].LevelData.holderSlotData[j].position.y, z * 2);
                detailData[i].LevelData.holderSlotData[j].position = newPos;
            }

            for (int j = 0; j < detailData[i].LevelData.staticRiceData.Count; j++)
            {
                int x = Mathf.RoundToInt(detailData[i].LevelData.staticRiceData[j].position.x / 2);
                int z = Mathf.RoundToInt(detailData[i].LevelData.staticRiceData[j].position.z / 2);
                Vector3 newPos = new Vector3(x * 2, detailData[i].LevelData.staticRiceData[j].position.y, z * 2);
                detailData[i].LevelData.staticRiceData[j].position = newPos;
            }
        }
        EditorUtility.SetDirty(this);
    }
    #endregion
#endif

    [Serializable]
    public class Data
    {
        [TableColumnWidth(100, false)]
        public string LevelName;
        public LevelData LevelData;
    }
}



