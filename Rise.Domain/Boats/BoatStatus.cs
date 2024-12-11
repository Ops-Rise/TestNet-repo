namespace Rise.Domain.Boats
{
    public enum BoatStatus
    {
        Available,
        InRepair,
        OutOfService,
    }

    public static class BoatExtension
    {
        public static string TranslateStatusToNL(this BoatStatus status)
        {
            switch (status)
            {
                case BoatStatus.Available:
                    return "Beschikbaar";
                case BoatStatus.InRepair:
                    return "In reparatie";
                case BoatStatus.OutOfService:
                    return "Buiten dienst";
                default:
                    return "Onbekend";
            }
        }
    }
}