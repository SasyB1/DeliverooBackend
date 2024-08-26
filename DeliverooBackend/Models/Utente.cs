namespace DeliverooBackend.Models
{
    public enum RuoloUtente
    {
        Ospite,
        Admin,
        Ristoratore
    }
    public class Utente
    {
        public int ID_Utente { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Indirizzo { get; set; }
        public string PasswordHash { get; set; }
        public string AccessToken { get; set; }
        public DateTime? DataScadenzaToken { get; set; }
        public RuoloUtente Ruolo { get; set; }
    }
}
