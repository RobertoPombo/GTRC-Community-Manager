using System;

namespace Enums
{
    public enum TimeTypeEnum
    {
        Miliseconds = 0,
        Seconds = 1,
        Minutes = 2,
        Hours = 3,
        Days = 4,
        Weeks = 5,
        Months = 6,
        Years = 7
    }

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

    public enum IncidentsStatusEnum
    {
        Open = 0,
        DoneLive = 1,
        DonePostRace = 2,
        Discarded = 3
    }

    public enum ReportReasonEnum
    {
        ManualReport = 0,
        Collision = 1,
        ReturnToGarage = 2,
        WrongStartingPos = 3,
        OvertakeQuali = 4,
        OvertakeOffTrack = 5,
        DriverReport = 6
    }

    public enum IncidentPropCategoryEnum
    {
        OriginalReport = 0,
        Report = 1,
        Description = 2,
        Decision = 3,
        Status = 4
    }

    public enum FormationLapTypeEnum
    {
        Manual = 0,
        ManualShort = 1,
        Controlled = 2,
        ControlledShort = 3,
        Limiter = 4,
        LimiterShort = 5
    }

    public enum DayOfWeekendEnum
    {
        Friday = 1,
        Saturday = 2,
        Sunday = 3
    }
}
