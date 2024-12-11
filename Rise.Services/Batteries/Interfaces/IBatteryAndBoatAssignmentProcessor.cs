namespace Rise.Services.Batteries
{
    public interface IBatteryAndBoatAssignmentProcessor
    {
        /// <summary>
        /// Start het proces om batterijen toe te wijzen aan boekingen.
        /// </summary>


        Task ProcessBatteryAndBoatAssignmentsAsync();
    }
}
