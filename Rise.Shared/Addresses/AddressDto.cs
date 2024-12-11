using System.Text.Json.Serialization;
namespace Rise.Shared.Addresses
{
    public class AddressDto
    {
        public int Id { get; set; }
        [JsonPropertyName("street")]
        public string? Street { get; set; }
        [JsonPropertyName("houseNumber")]
        public string? HouseNumber { get; set; }
        [JsonPropertyName("unitNumber")]
        public string? UnitNumber { get; set; }
        [JsonPropertyName("city")]
        public string? City { get; set; }
        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }
        public string FullAddress =>
            $"{Street} {HouseNumber} "
            + (string.IsNullOrEmpty(UnitNumber) ? "" : $"bus {UnitNumber} ")
            + $"{PostalCode} {City}";
    }
}
