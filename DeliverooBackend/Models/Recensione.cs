namespace DeliverooBackend.Models
{
    public class Recensione
    {
        public int ID_Recensione { get; set; }
        public int ID_Utente { get; set; }
        public string NomeUtente { get; set; }
        public int Valutazione { get; set; }
        public string Commento { get; set; }
        public DateTime DataRecensione { get; set; }
    }
}
