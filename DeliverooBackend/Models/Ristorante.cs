namespace DeliverooBackend.Models
{
    public class Ristorante
    {
        public int ID_Ristorante { get; set; }
        public string Nome { get; set; }
        public string Indirizzo { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public int ID_Utente { get; set; }
        public decimal Latitudine { get; set; }
        public decimal Longitudine { get; set; }
        public List<OrarioApertura> OrariApertura { get; set; }
        public string? ImmaginePath { get; set; }
    }
}