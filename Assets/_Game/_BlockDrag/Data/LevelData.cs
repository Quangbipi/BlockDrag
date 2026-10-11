using Quang.DesignPattern;
using System;
using System.Collections.Generic;
using UnityEngine;

// Level layout types restored from the pre-migration Assets/_Game/_Base/GameData.cs (Base namespace).
// com.quang.base only ships GameData.LevelData (player progress), so the level authoring schema lives here.
namespace Gameplay.BlockDrag
{
    public enum DIRECTION
    {
        NONE = -1,
        LEFT = 0,
        UP = 1,
        RIGHT = 2,
        DOWN = 3,
    }

    public enum PLACE_TYPE
    {
        NONE = -1,
        NORMAL = 0,
        SHRIMP = 1,
        STRAWBERY = 2,
        DANGO_1 = 3,
        DANGO_2 = 4,
        DANGO_3 = 5,
        CAKE_1 = 6,
        CAKE_2 = 7,
        CAKE_3 = 8,
        CAKE_4 = 9,
    }

    public enum LEVEL_DIFFICULTY
    {
        NONE = -1,
        EASY = 0,
        MEDIUM = 1,
        HARD = 2,
    }

    public enum LEVEL_PERFORMANCE_TYPE
    {
        NONE = -1,
        TIME = 0,
        CUTTING_TIME = 1,
    }

    [Serializable]
    public class LevelData
    {
        public int index;
        public Vector2Int size = new Vector2Int(8, 14);
        public LEVEL_DIFFICULTY difficulty;
        public LEVEL_PERFORMANCE_TYPE performanceType;
        public int maxCutPerformanceRequire = -1;
        public int maxKnifeCut;
        public List<int> times = new List<int> { 60, 180, 300 };
        public List<KnifeUnitData> knifeData;
        public List<HolderSlotData> holderSlotData;
        public List<RiceUnitData> riceData;
        public List<StaticRiceUnitData> staticRiceData;
        public List<MovingBeltSlotData> movingBeltSlotData;

        public LevelData() { }
        public LevelData(int index, Vector2Int size, LEVEL_DIFFICULTY difficulty, LEVEL_PERFORMANCE_TYPE performanceType, int maxCutPerformanceRequire, int maxKnifeCut, List<int> times, List<HolderSlotData> holderSlotData, List<RiceUnitData> riceData
            , List<KnifeUnitData> knifeData, List<StaticRiceUnitData> staticRiceData, List<MovingBeltSlotData> movingBeltSlotData)
        {
            this.index = index;
            this.size = size;
            this.difficulty = difficulty;
            this.performanceType = performanceType;
            this.maxCutPerformanceRequire = maxCutPerformanceRequire;
            this.maxKnifeCut = maxKnifeCut;
            this.times = times;
            this.holderSlotData = new List<HolderSlotData>(holderSlotData);
            this.riceData = new List<RiceUnitData>(riceData);
            this.knifeData = new List<KnifeUnitData>(knifeData);
            this.staticRiceData = new List<StaticRiceUnitData>(staticRiceData);
        }
    }

    [Serializable]
    public class UnitData
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public Vector3 boxColliderSize;

        public UnitData(Transform tf, Vector3 boxColliderSize)
        {
            this.position = tf.position;
            this.rotation = tf.rotation.eulerAngles;
            this.scale = tf.localScale;
            this.boxColliderSize = boxColliderSize;
        }
    }

    [Serializable]
    public class RiceUnitData : UnitData
    {
        public List<DIRECTION> linkedDirections; // LEFT, UP, RIGHT, DOWN
        public List<DIRECTION> blockDirections;
        public PoolType poolType;
        public PLACE_TYPE placeType;
        public int capacity;

        public RiceUnitData(Transform tf, Vector3 boxColliderSize, PoolType poolType
        , List<DIRECTION> linkedDirections = null, List<DIRECTION> blockDirections = null, PLACE_TYPE placeType = PLACE_TYPE.NONE) : base(tf, boxColliderSize)
        {
            this.poolType = poolType;
            this.linkedDirections = linkedDirections;
            this.blockDirections = blockDirections;
            this.placeType = placeType;
        }
    }

    [Serializable]
    public class KnifeUnitData : UnitData
    {
        public DIRECTION cutDirection;
        public List<DIRECTION> linkedDirections; // LEFT, UP, RIGHT, DOWN

        public KnifeUnitData(Transform tf, Vector3 boxColliderSize, DIRECTION cutDirection
        , List<DIRECTION> linkedDirections = null) : base(tf, boxColliderSize)
        {
            this.cutDirection = cutDirection;
            this.linkedDirections = linkedDirections;
        }
    }

    [Serializable]
    public class StaticRiceUnitData : UnitData
    {
        public StaticRiceUnitData(Transform tf, Vector3 boxCollideSize) : base(tf, boxCollideSize)
        {
        }
    }

    [Serializable]
    public class HolderSlotData : UnitData
    {
        public int hintIndex;
        public PLACE_TYPE placeType;
        public List<DIRECTION> blockDirections; // LEFT, UP, RIGHT, DOWN

        public HolderSlotData(Transform tf, Vector3 boxCollideSize, int hintIndex, List<DIRECTION> blockDirections, PLACE_TYPE placeType) : base(tf, boxCollideSize)
        {
            this.blockDirections = blockDirections;
            this.placeType = placeType;
            this.hintIndex = hintIndex;
        }
    }

    [Serializable]
    public class MovingBeltSlotData : UnitData
    {
        public List<RiceUnitData> vehicleData;
        public int vehicleShowCount;

        public MovingBeltSlotData(List<RiceUnitData> vehicleData, Transform tf, Vector3 boxCollideSize, int showVehicleCount) : base(tf, boxCollideSize)
        {
            this.vehicleData = vehicleData;
            this.vehicleShowCount = showVehicleCount;
        }
    }
}
