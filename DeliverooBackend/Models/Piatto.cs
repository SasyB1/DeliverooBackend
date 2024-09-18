﻿namespace DeliverooBackend.Models
{
    public class Piatto
    {
        public int ID_Piatto { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public decimal Prezzo { get; set; }
        public int ID_Menu { get; set; }
        public string ImmaginePath { get; set; }
        public bool Cancellato { get; set; }
        public bool ConsenteIngredienti { get; set; }
    }
}