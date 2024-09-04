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

    public List<Ristorante> GetRistorantiVicini(decimal latitudineUtente, decimal longitudineUtente, decimal distanzaMassimaKm)
    {
        var ristoranti = RecuperaRistorantiDalDatabase();
        var ristorantiVicini = new List<Ristorante>();

        foreach (var ristorante in ristoranti)
        {
            decimal distanza = CalcolaDistanza(latitudineUtente, longitudineUtente, ristorante.Latitudine, ristorante.Longitudine);
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
            string query = "SELECT ID_Ristorante, Nome, Indirizzo, Telefono, Email, Latitudine, Longitudine,ImmaginePath FROM Ristoranti";
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
                            Latitudine = reader.GetDecimal(5),
                            Longitudine = reader.GetDecimal(6),
                            ImmaginePath = reader.IsDBNull(7) ? null : reader.GetString(7)
                        };
                        ristoranti.Add(ristorante);
                    }
                }
            }
        }
        return ristoranti;
    }


    public decimal CalcolaDistanza(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        decimal lat1Rad = lat1 * (decimal)Math.PI / 180;
        decimal lon1Rad = lon1 * (decimal)Math.PI / 180;
        decimal lat2Rad = lat2 * (decimal)Math.PI / 180;
        decimal lon2Rad = lon2 * (decimal)Math.PI / 180;

        // Formula di Haversine
        decimal dLat = lat2Rad - lat1Rad;
        decimal dLon = lon2Rad - lon1Rad;

        decimal a = (decimal)Math.Sin((double)dLat / 2) * (decimal)Math.Sin((double)dLat / 2) +
                    (decimal)Math.Cos((double)lat1Rad) * (decimal)Math.Cos((double)lat2Rad) *
                    (decimal)Math.Sin((double)dLon / 2) * (decimal)Math.Sin((double)dLon / 2);

        decimal c = 2 * (decimal)Math.Atan2((double)Math.Sqrt((double)a), (double)Math.Sqrt((double)(1 - a)));

        decimal r = 6371;

        return r * c;
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
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(immagine.FileName);
            immaginePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(immaginePath, FileMode.Create))
            {
                await immagine.CopyToAsync(fileStream);
            }

            immaginePath = "/uploads/" + fileName;
            ristorante.ImmaginePath = immaginePath;

            Console.WriteLine($"Immagine salvata correttamente con il percorso: {immaginePath}");
        }
        else
        {
            Console.WriteLine("Nessuna immagine caricata.");
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
            Console.WriteLine($"Ristorante creato con ID: {newId} e immaginePath: {ristorante.ImmaginePath}");
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