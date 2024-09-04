using DeliverooBackend.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

public class RistoranteService
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;

    public RistoranteService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _environment = environment;
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

    public async Task CreaRistorante(Ristorante ristorante, IFormFile immagine)
    {
        Console.WriteLine("Dati ricevuti:");
        Console.WriteLine(JsonConvert.SerializeObject(ristorante, Formatting.Indented));
        if (ristorante.OrariApertura == null || ristorante.OrariApertura.Count == 0)
        {
            Console.WriteLine("Nessun orario di apertura fornito.");
            ristorante.OrariApertura = new List<OrarioApertura>();
        }

        string immaginePath = null;

        if (immagine != null && immagine.Length > 0)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            immaginePath = Path.Combine(uploadsFolder, immagine.FileName);

            using (var fileStream = new FileStream(immaginePath, FileMode.Create))
            {
                await immagine.CopyToAsync(fileStream);
            }

            immaginePath = "/uploads/" + immagine.FileName;
            ristorante.ImmaginePath = immaginePath;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string query = @"
            INSERT INTO Ristoranti (Nome, Indirizzo, Telefono, Email, ID_Utente, Latitudine, Longitudine, ImmaginePath)
            VALUES (@Nome, @Indirizzo, @Telefono, @Email, @ID_Utente, @Latitudine, @Longitudine, @ImmaginePath);
            SELECT SCOPE_IDENTITY();"; 

            int newId;

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Nome", ristorante.Nome);
                cmd.Parameters.AddWithValue("@Indirizzo", ristorante.Indirizzo);
                cmd.Parameters.AddWithValue("@Telefono", ristorante.Telefono);
                cmd.Parameters.AddWithValue("@Email", ristorante.Email);
                cmd.Parameters.AddWithValue("@ID_Utente", ristorante.ID_Utente);
                cmd.Parameters.AddWithValue("@Latitudine", ristorante.Latitudine);
                cmd.Parameters.AddWithValue("@Longitudine", ristorante.Longitudine);
                cmd.Parameters.AddWithValue("@ImmaginePath", (object)ristorante.ImmaginePath ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                newId = Convert.ToInt32(result);
            }

            ristorante.ID_Ristorante = newId;
            if (ristorante.ID_Ristorante > 0)
            {
                await InserisciOrariApertura(conn, ristorante.ID_Ristorante, ristorante.OrariApertura);
            }
            else
            {
                Console.WriteLine("Errore: l'ID del ristorante non è stato assegnato correttamente.");
            }
        }
    }




    private async Task InserisciOrariApertura(SqlConnection conn, int idRistorante, List<OrarioApertura> orariApertura)
    {
        if (orariApertura == null || !orariApertura.Any())
        {
            Console.WriteLine("Nessun orario di apertura fornito.");
            return;
        }

        string query = @"
    INSERT INTO OrariApertura (ID_Ristorante, GiornoSettimana, OraApertura, OraChiusura)
    VALUES (@ID_Ristorante, @GiornoSettimana, @OraApertura, @OraChiusura);";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            foreach (var orario in orariApertura)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                cmd.Parameters.AddWithValue("@GiornoSettimana", orario.GiornoSettimana);
                cmd.Parameters.AddWithValue("@OraApertura", orario.OraApertura);
                cmd.Parameters.AddWithValue("@OraChiusura", orario.OraChiusura);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}