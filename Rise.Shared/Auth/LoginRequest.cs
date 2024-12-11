namespace Rise.Shared.Auth
{
    using System.ComponentModel.DataAnnotations;

    public class LoginRequest
    {
        [Required(ErrorMessage = "E-mailadres is vereist.")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mailadres.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Wachtwoord is vereist.")]
        [MinLength(8, ErrorMessage = "Wachtwoord moet minimaal 8 tekens lang zijn.")]
        public string Password { get; set; }
    }

}