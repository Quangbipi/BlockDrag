using System;
using System.Collections.Generic;

namespace Base
{
    [Serializable]
    public class DailyGiftDbModel
    {
        public int year;
        public int dayOfYear;
        public int dayCount;
        public List<bool> listDailyGiftStatus;

        public static DailyGiftDbModel Load() { return Database.Load<DailyGiftDbModel>(); }
        public void Save() { Database.Save<DailyGiftDbModel>(this); }
    }
}