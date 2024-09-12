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
    public List<Ristorante> GetRestaurantsByUserId(int iD_Utente)
    {
        var ristoranti = new List<Ristorante>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "SELECT ID_Ristorante, Nome, Indirizzo, Telefono, Email, Latitudine, Longitudine, ImmaginePath FROM Ristoranti WHERE ID_Utente = @ID_Utente";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ID_Utente", iD_Utente);

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

    public async Task CreaMenu(Menu menu)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string query = "INSERT INTO Menu (Nome, ID_Ristorante) VALUES (@Nome, @ID_Ristorante)";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Nome", menu.Nome);
                cmd.Parameters.AddWithValue("@ID_Ristorante", menu.ID_Ristorante);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task CreaPiatto(Piatto piatto, IFormFile immagine)
    {
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
            piatto.ImmaginePath = immaginePath;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string query = "INSERT INTO Piatti (Nome, Descrizione, Prezzo, ID_Menu, ImmaginePath) VALUES (@Nome, @Descrizione, @Prezzo, @ID_Menu, @ImmaginePath)";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Nome", piatto.Nome);
                cmd.Parameters.AddWithValue("@Descrizione", piatto.Descrizione);
                cmd.Parameters.AddWithValue("@Prezzo", piatto.Prezzo);
                cmd.Parameters.AddWithValue("@ID_Menu", piatto.ID_Menu);
                cmd.Parameters.AddWithValue("@ImmaginePath", (object)piatto.ImmaginePath ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }



    public List<Menu> GetMenusByRestaurantId(int idRistorante)
    {
        var menus = new List<Menu>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "SELECT ID_Menu, Nome FROM Menu WHERE ID_Ristorante = @ID_Ristorante AND Cancellato = 0";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var menu = new Menu
                        {
                            ID_Menu = reader.GetInt32(0),
                            Nome = reader.GetString(1)
                        };
                        menus.Add(menu);
                    }
                }
            }
        }
        return menus;
    }


    public List<Piatto> GetPiattiByMenuId(int idMenu)
    {
        var piatti = new List<Piatto>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "SELECT ID_Piatto, Nome, Descrizione, Prezzo, ImmaginePath, Cancellato FROM Piatti WHERE ID_Menu = @ID_Menu";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ID_Menu", idMenu);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var piatto = new Piatto
                        {
                            ID_Piatto = reader.GetInt32(0),
                            Nome = reader.GetString(1),
                            Descrizione = reader.GetString(2),
                            Prezzo = reader.GetDecimal(3),
                            ImmaginePath = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Cancellato = reader.IsDBNull(5) ? false : reader.GetBoolean(5)
                        };
                        piatti.Add(piatto);
                    }
                }
            }
        }
        return piatti;
    }


    public async Task AggiornaCategorieRistorante(int idRistorante, List<int> selectedCategories)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string deleteQuery = @"
        DELETE FROM RistoranteCategorie
        WHERE ID_Ristorante = @ID_Ristorante
        AND ID_Categoria NOT IN (" + string.Join(",", selectedCategories) + ")";

            using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
            {
                deleteCmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                await deleteCmd.ExecuteNonQueryAsync();
            }
            foreach (var categoryId in selectedCategories)
            {
                string checkQuery = @"
            SELECT COUNT(*)
            FROM RistoranteCategorie
            WHERE ID_Ristorante = @ID_Ristorante AND ID_Categoria = @ID_Categoria";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                    checkCmd.Parameters.AddWithValue("@ID_Categoria", categoryId);

                    var count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        string insertQuery = @"
                    INSERT INTO RistoranteCategorie (ID_Ristorante, ID_Categoria)
                    VALUES (@ID_Ristorante, @ID_Categoria)";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                            insertCmd.Parameters.AddWithValue("@ID_Categoria", categoryId);
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }
    }



    public List<Categoria> GetCategorie()
    {
        var categorie = new List<Categoria>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "SELECT ID_Categoria, Nome FROM CategorieRistoranti";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var categoria = new Categoria
                        {
                            ID_Categoria = reader.GetInt32(0),
                            Nome = reader.GetString(1)
                        };
                        categorie.Add(categoria);
                    }
                }
            }
        }
        return categorie;
    }
    public List<int> GetCategorieAssociate(int idRistorante)
    {
        var categorieAssociate = new List<int>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
            SELECT ID_Categoria 
            FROM RistoranteCategorie 
            WHERE ID_Ristorante = @ID_Ristorante";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categorieAssociate.Add(reader.GetInt32(0));
                    }
                }
            }
        }

        return categorieAssociate;
    }
    public async Task<bool> DeletePiatto(int idPiatto)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string query = "UPDATE Piatti SET Cancellato = 1 WHERE ID_Piatto = @ID_Piatto";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ID_Piatto", idPiatto);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }

    public async Task<bool> DeleteMenu(int idMenu)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string updatePiattiQuery = "UPDATE Piatti SET Cancellato = 1 WHERE ID_Menu = @ID_Menu";
            using (SqlCommand cmdUpdatePiatti = new SqlCommand(updatePiattiQuery, conn))
            {
                cmdUpdatePiatti.Parameters.AddWithValue("@ID_Menu", idMenu);
                await cmdUpdatePiatti.ExecuteNonQueryAsync();
            }
            string updateMenuQuery = "UPDATE Menu SET Cancellato = 1 WHERE ID_Menu = @ID_Menu";
            using (SqlCommand cmdUpdateMenu = new SqlCommand(updateMenuQuery, conn))
            {
                cmdUpdateMenu.Parameters.AddWithValue("@ID_Menu", idMenu);
                int rowsAffected = await cmdUpdateMenu.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }

    public async Task<bool> AggiornaMenu(int idMenu, string nuovoNome)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            string query = "UPDATE Menu SET Nome = @Nome WHERE ID_Menu = @ID_Menu";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Nome", nuovoNome);
                cmd.Parameters.AddWithValue("@ID_Menu", idMenu);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }

    public async Task<bool> AggiornaPiatto(int idPiatto, string nome, string descrizione, decimal prezzo, IFormFile immagine)
    {
        string immaginePath = null;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            string selectQuery = "SELECT ImmaginePath FROM Piatti WHERE ID_Piatto = @ID_Piatto";
            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
            {
                selectCmd.Parameters.AddWithValue("@ID_Piatto", idPiatto);
                var result = await selectCmd.ExecuteScalarAsync();
                immaginePath = result as string;
            }
            if (immagine != null && immagine.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(immagine.FileName);
                string newImmaginePath = Path.Combine(uploadsFolder, fileName);
                using (var fileStream = new FileStream(newImmaginePath, FileMode.Create))
                {
                    await immagine.CopyToAsync(fileStream);
                }

                immaginePath = "/uploads/" + fileName; 

                Console.WriteLine($"Nuova immagine salvata in: {immaginePath}");
            }
            else
            {
                Console.WriteLine("Nessuna nuova immagine fornita, mantengo l'immagine precedente.");
            }
            string query = @"
            UPDATE Piatti
            SET Nome = @Nome, Descrizione = @Descrizione, Prezzo = @Prezzo, ImmaginePath = @ImmaginePath
            WHERE ID_Piatto = @ID_Piatto";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Nome", nome);
                cmd.Parameters.AddWithValue("@Descrizione", descrizione);
                cmd.Parameters.AddWithValue("@Prezzo", prezzo);
                cmd.Parameters.AddWithValue("@ImmaginePath", immaginePath);
                cmd.Parameters.AddWithValue("@ID_Piatto", idPiatto);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }

}