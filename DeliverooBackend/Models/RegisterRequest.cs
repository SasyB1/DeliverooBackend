namespace DeliverooBackend.Models
{
    public class RegisterRequest
    {
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Password { get; set; }
        public string Ruolo { get; set; }
    }
}
