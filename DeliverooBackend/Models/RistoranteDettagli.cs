namespace DeliverooBackend.Models
{
    public class RistoranteDettagli
    {
        public Ristorante Ristorante { get; set; } = new Ristorante();
        public List<Menu> Menus { get; set; } = new List<Menu>();
        public List<Promozione> Promozioni { get; set; } = new List<Promozione>();
        public List<Categoria> Categorie { get; set; } = new List<Categoria>();

    }

}
