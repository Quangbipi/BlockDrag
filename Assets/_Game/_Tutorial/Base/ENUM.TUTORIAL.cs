using UnityEngine;

namespace Base
{
    public enum HAND_ACTION
    {
        NONE = -1,
        POINT_OUT = 0,
        MOVE_DRAG = 1,
        ZOOM_IN = 3,
        ZOOM_OUT = 4,
    }

    public enum TUTORIAL_ACTION
    {
        NONE = -1,
        HAND = 0,
        SHOW_VIDEO = 1,
        CALL_OUT = 2,
        END_TUTORIAL = 3,
        SHOW_CONTENT = 4,
    }
    public enum CUSTOM_ACTION
    {
        NONE = -1,
        SHOW_ALL_HINT = 0,
        HIDE_HINT_BOOSTER = 1,
        HIDE_KNIFE_BOOSTER = 2,
        SHOW_HINT_BOOSTER = 3,
        SHOW_KNIFE_BOOSTER = 4,
        GET_HINT_BOOSTER_INFO = 5,
        GET_KNIFE_BOOSTER_INFO = 6,
        ADD_HINT_BOOSTER = 7,
        ADD_KNIFE_BOOSTER = 8,
        UPDATE_UI = 9,
        GET_HAND_INFO = 10,
    }
    public enum CUSTOM_CONDITION
    {
        NONE = -1,
        START_LEVEL = 0,
        RICE_CLUSTER_START_DRAG = 1,
        CONFIRM_BUTTON_CLICK = 2,
        BOOSTER_BUTTON_CLICK = 3,
    }
    public enum POSITION_TYPE
    {
        NONE = -1,
        WORLD_POS = 0,
        UI_POS = 1,
    }
}
