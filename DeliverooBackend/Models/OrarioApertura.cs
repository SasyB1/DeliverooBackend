namespace DeliverooBackend.Models
{
    public class OrarioApertura
    {
        public int ID_OrarioApertura { get; set; }
        public int GiornoSettimana { get; set; }
        public TimeSpan OraApertura { get; set; }
        public TimeSpan OraChiusura { get; set; }
    }
}