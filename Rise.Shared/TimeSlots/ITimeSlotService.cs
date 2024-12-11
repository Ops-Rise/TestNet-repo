namespace Rise.Shared.TimeSlots
{
    public interface ITimeSlotService
    {
        Task<IEnumerable<TimeSlotDto>?> GetAllTimeSlotsAsync(DateTime startDate, DateTime endDate);
        Task BlockTimeSlotAsync(TimeSlotDto model);
        Task<bool> UnblockTimeSlotAsync(DateTime date, int timeSlot);
    }
}
