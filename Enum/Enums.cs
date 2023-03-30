using System;

namespace GTRC_Community_Manager
{
    public enum SessionTypeEnum
    {
        Practice = 0,
        Qualifying = 1,
        Race = 2
    }

    public enum ServerTypeEnum
    {
        PreQuali = 0,
        Practice = 1,
        Event = 2
    }

    public enum EntrylistTypeEnum
    {
        None = 0,
        RaceControl = 1,
        AllDrivers = 2,
        Season = 3
    }
}
