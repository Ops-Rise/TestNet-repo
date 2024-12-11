using Rise.Domain.Bookings;
using Rise.Domain.Users;

namespace Rise.Domain.Batteries
{
    public class Battery : Entity
    {
        private string name = default!;

        public string Name
        {
            get => name;
            set => name = Guard.Against.NullOrWhiteSpace(value, nameof(Name));
        }

        private BatteryStatus status = default!;

        public BatteryStatus Status
        {
            get => status;
            set => status = value;
        }

        public int UserId { get; set; }

        private User user = default!;

        public User User
        {
            get => user;
            set => user = Guard.Against.Null(value, nameof(value));
        }

        public ICollection<Booking> Bookings { get; } = new List<Booking>();

        private Battery() { }

        public Battery(string name, BatteryStatus status, User user)
        {
            Name = name;
            Status = status;
            User = user;
        }
    }
}
