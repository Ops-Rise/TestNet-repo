using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rise.Domain.Batteries
{
    public enum BatteryStatus
    {
        Available,
        InRepair,
        OutOfService,
        Reserve,
    }

    public static class BatteryExtension
    {
        public static string TranslateStatusToNL(this BatteryStatus status)
        {
            switch (status)
            {
                case BatteryStatus.Available:
                    return "Beschikbaar";
                case BatteryStatus.InRepair:
                    return "In reparatie";
                case BatteryStatus.OutOfService:
                    return "Buiten dienst";
                case BatteryStatus.Reserve:
                    return "Reserve";
                default:
                    return "Onbekend";
            }
        }
    }
}