namespace Rise.Shared.Emails.Models;

public class BookingConfirmedOrCanceledEmailModel
{
    public string? FirstName { get; set; }
    public DateTime RentalDate { get; set; }
    public string BookingId { get; set; }
    public string? Remark { get; set; }
}
