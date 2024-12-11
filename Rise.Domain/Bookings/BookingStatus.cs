using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rise.Domain.Bookings
{
    public enum BookingStatus
    {
        Active,
        Completed,
        Canceled,
    }

    public static class BookingExtension
    {
        public static string TranslateStatusToNL(this BookingStatus status)
        {
            switch (status)
            {
                case BookingStatus.Canceled:
                    return "Geannuleerd";
                case BookingStatus.Completed:
                    return "Voltooid";
                case BookingStatus.Active:
                    return "Actief";
                default:
                    return "Onbekend";
            }
        }
    }
    
}
