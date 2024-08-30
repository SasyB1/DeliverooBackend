
using DeliverooBackend.Models;
using Microsoft.Data.SqlClient;

namespace DeliverooBackend.Services
{
    public class RistoranteService
    {
        private readonly string _connectionString;

        public RistoranteService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<Ristorante> GetRistorantiVicini(double latitudineUtente, double longitudineUtente, double distanzaMassimaKm)
        {
            var ristoranti = RecuperaRistorantiDalDatabase();
            var ristorantiVicini = new List<Ristorante>();

            foreach (var ristorante in ristoranti)
            {
                double distanza = CalcolaDistanza(latitudineUtente, longitudineUtente, ristorante.Latitudine, ristorante.Longitudine);
                if (distanza <= distanzaMassimaKm)
                {
                    ristorantiVicini.Add(ristorante);
                }
            }

            return ristorantiVicini;
        }

        private List<Ristorante> RecuperaRistorantiDalDatabase()
        {
            var ristoranti = new List<Ristorante>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT ID_Ristorante, Nome, Indirizzo, Telefono, Email, Latitudine, Longitudine FROM Ristoranti";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ristorante = new Ristorante
                            {
                                ID_Ristorante = reader.GetInt32(0),
                                Nome = reader.GetString(1),
                                Indirizzo = reader.GetString(2),
                                Telefono = reader.GetString(3),
                                Email = reader.GetString(4),
                                Latitudine = Convert.ToDouble(reader.GetDecimal(5)), 
                                Longitudine = Convert.ToDouble(reader.GetDecimal(6))
                            };
                            ristoranti.Add(ristorante);
                        }
                    }
                }
            }
            return ristoranti;
        }


        private double CalcolaDistanza(double lat1, double lon1, double lat2, double lon2)
        {
            const double RaggioTerraKm = 6371.0;
            double dLat = GradiARadianti(lat2 - lat1);
            double dLon = GradiARadianti(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(GradiARadianti(lat1)) * Math.Cos(GradiARadianti(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return RaggioTerraKm * c;
        }

        private double GradiARadianti(double gradi)
        {
            return gradi * (Math.PI / 180);
        }
    }
}
