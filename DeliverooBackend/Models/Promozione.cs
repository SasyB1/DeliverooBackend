namespace DeliverooBackend.Models
{
    public class Promozione
    {
        public int ID_Promozione { get; set; } 
        public int ID_Ristorante { get; set; } 
        public string Descrizione { get; set; } 
        public DateTime DataInizio { get; set; } 
        public DateTime DataFine { get; set; } 
        public decimal ScontoPercentuale { get; set; } 
    }

}
