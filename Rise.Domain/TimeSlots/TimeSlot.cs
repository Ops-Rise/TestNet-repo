using Rise.Domain.Users;

namespace Rise.Domain.TimeSlots;

public class TimeSlot
{
    public enum TimeSlotType
    {
        ochtend = 0, // 09:00
        middag = 1, // 12:00
        namiddag =
            2 // 15:00
        ,
    }

    public int Id { get; set; }
    public DateTime Date { get; set; }

    public TimeSlotType Type { get; set; }
    public string? Reason { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
}
