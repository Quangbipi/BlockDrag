using UnityEngine;

namespace Base
{
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
    public enum SURFACE_TYPE
    {
        NONE = -1,
        GROUND = 0,
    }
    public enum SURFACE_STATE
    {
        NONE = -1,
        NORMAL = 0,
        WET = 1,
        DRY = 2,
        FERTILIZER = 3,
        SELECTED = 100,
    }
}
