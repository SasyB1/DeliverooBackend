namespace DeliverooBackend.Models
{
    public class Menu
    {
        public int ID_Menu { get; set; }  
        public string Nome { get; set; }  
        public int ID_Ristorante { get; set; } 
        public bool Cancellato { get; set; } 
        public List<Piatto> Piatti { get; set; } = new List<Piatto>(); 
    }
}
