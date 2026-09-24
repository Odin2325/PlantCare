namespace PlantCare.Domain.Enums;

public enum CareScheduleMode
{
    Interval = 0,
    Weekdays = 1
}

[Flags]
public enum CareWeekDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}
