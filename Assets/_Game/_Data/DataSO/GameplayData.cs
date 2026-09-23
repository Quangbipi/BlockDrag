using Base;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DesignPattern;

[CreateAssetMenu(fileName = "GameplayData", menuName = "ScriptableObjects/Gameplay")]
public class GameplayData : SerializedScriptableObject
{
    [SerializeField]
    private Dictionary<int, Vector2> vehicleSize;
    public Dictionary<int, Vector2> VehicleSize => vehicleSize;
    [SerializeField]
    private Dictionary<SURFACE_STATE, Material> surfaceMaterial;
    public Dictionary<SURFACE_STATE, Material> SurfaceMaterial => surfaceMaterial;
    [SerializeField]
    private Dictionary<ITEM, ItemData> items;
    public Dictionary<ITEM, ItemData> Items => items;
    [SerializeField]
    private Dictionary<ITEM_RARITY, Sprite> frames;
    public Dictionary<ITEM_RARITY, Sprite> Frames => frames;
    [SerializeField]
    private List<PoolType> emojis;
    public List<PoolType> Emojis => emojis;
    [SerializeField]
    private int reviveHeartCost;
    public int ReviveHeartCost => reviveHeartCost;
    [SerializeField]
    private int winLevelGold;
    public int WinLevelGold => winLevelGold;
    [SerializeField]
    private int maxHearts;
    public int MaxHearts => maxHearts;
    [SerializeField]
    private float heartCooldownTime;
    public float HeartCooldownTime => heartCooldownTime;
    [SerializeField]
    private int levelPerChapter;
    public int LevelPerChapter => levelPerChapter;
    [SerializeField]
    protected Vector2Int extentSurfaceSize;
    public Vector2Int ExtentSurfaceSize => extentSurfaceSize;
    [SerializeField]
    protected Dictionary<PLACE_TYPE, PlaceTypeData> placeTypeData;
    public Dictionary<PLACE_TYPE, PlaceTypeData> PlaceTypeData => placeTypeData;
    [SerializeField]
    protected List<List<PLACE_TYPE>> groupedPlaceTypes;
    public List<List<PLACE_TYPE>> GroupedPlaceTypes => groupedPlaceTypes;
    [SerializeField]
    protected List<Sprite> toppingLinkSprites;
    public List<Sprite> ToppingLinkSprites => toppingLinkSprites;
    [SerializeField]
    protected List<Vector3> toppingMainSpriteSizes;
    public List<Vector3> ToppingMainSpriteSizes => toppingMainSpriteSizes;
    [SerializeField]
    protected List<List<Vector3>> toppingLinkOffsets;
    public List<List<Vector3>> ToppingLinkOffsets => toppingLinkOffsets;
    [SerializeField]
    protected List<List<Vector3>> toppingLinkRotations;
    public List<List<Vector3>> ToppingLinkRotations => toppingLinkRotations;
    [SerializeField]
    protected List<List<Vector2>> toppingLinkSizes;
    public List<List<Vector2>> ToppingLinkSizes => toppingLinkSizes;

}
