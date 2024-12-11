namespace Rise.Shared.Prices
{
    public abstract class PriceDto
    {
        public class Index
        {
            public required int Id { get; set; }

            public required decimal Amount { get; set; }

        }

        public class History : Index
        {
            public required DateTime CreatedAt { get; set; }
        }

        public class Create
        {
            public required decimal Amount { get; set; }
        }
    }
}
