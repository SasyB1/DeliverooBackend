namespace DeliverooBackend.Models
{
    public class DettaglioOrdine
    {
        public int ID_DettaglioOrdine { get; set; }  
        public int ID_Ordine { get; set; }         
        public Piatto Piatto { get; set; }         
        public int Quantita { get; set; }            
        public decimal Prezzo { get; set; }        
        public List<IngredienteDettaglio> Ingredienti { get; set; } = new List<IngredienteDettaglio>();
    }


}
