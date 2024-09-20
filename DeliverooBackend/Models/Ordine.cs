namespace DeliverooBackend.Models
{
    public class Ordine
    {
        public int ID_Ordine { get; set; }
        public DateTime DataOrdine { get; set; }
        public string Stato { get; set; }
        public int ID_Utente { get; set; }
        public int ID_Ristorante { get; set; }
        public List<DettaglioOrdine> DettagliOrdine { get; set; } = new List<DettaglioOrdine>();

        public string NomeRistorante { get; set; }
    }


}
