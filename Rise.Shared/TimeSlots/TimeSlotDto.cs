namespace Rise.Shared.TimeSlots
{
    public class TimeSlotDto
    {
        public int CreatedByUserId { get; set; }
        public DateTime Date { get; set; }
        public int TimeSlot { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
