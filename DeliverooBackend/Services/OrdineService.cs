using DeliverooBackend.Models;
using Microsoft.Data.SqlClient;

namespace DeliverooBackend.Services
{
    public class OrdineService
    {
        private readonly string _connectionString;

        public OrdineService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<int> CreaOrdine(int idUtente, int idRistorante)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
        INSERT INTO Ordini (ID_Utente, ID_Ristorante, DataOrdine, Stato)
        VALUES (@ID_Utente, @ID_Ristorante, @DataOrdine, @Stato);
        SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Utente", idUtente);
                    cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                    cmd.Parameters.AddWithValue("@DataOrdine", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Stato", "In Corso");

                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);  
                }
            }
        }



        public async Task AggiungiPiattoAOrdine(int idOrdine, int idPiatto, int quantita)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                decimal prezzoPiatto;
                string queryPrezzoPiatto = "SELECT Prezzo FROM Piatti WHERE ID_Piatto = @ID_Piatto";

                using (SqlCommand cmd = new SqlCommand(queryPrezzoPiatto, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Piatto", idPiatto);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        throw new Exception("Piatto non trovato nel database.");
                    }

                    prezzoPiatto = (decimal)result;  
                }

                string query = @"
        INSERT INTO DettagliOrdine (ID_Ordine, ID_Piatto, Quantità, Prezzo)
        VALUES (@ID_Ordine, @ID_Piatto, @Quantità, @Prezzo);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Ordine", idOrdine);
                    cmd.Parameters.AddWithValue("@ID_Piatto", idPiatto);
                    cmd.Parameters.AddWithValue("@Quantità", quantita);
                    cmd.Parameters.AddWithValue("@Prezzo", prezzoPiatto * quantita);  

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }



      
        public async Task AggiungiIngredienteADettaglioOrdine(int idDettaglioOrdine, int idIngrediente, int quantitaIngrediente)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                decimal prezzoIngrediente;
                string queryPrezzoIngrediente = "SELECT Prezzo FROM Ingredienti WHERE ID_Ingrediente = @ID_Ingrediente";

                using (SqlCommand cmd = new SqlCommand(queryPrezzoIngrediente, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Ingrediente", idIngrediente);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        throw new Exception("Ingrediente non trovato nel database.");
                    }
                    prezzoIngrediente = (decimal)result;
                }
                string query = @"
        INSERT INTO DettagliOrdineIngredienti (ID_DettaglioOrdine, ID_Ingrediente, QuantitàIngrediente, PrezzoIngrediente)
        VALUES (@ID_DettaglioOrdine, @ID_Ingrediente, @QuantitàIngrediente, @PrezzoIngrediente)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_DettaglioOrdine", idDettaglioOrdine);
                    cmd.Parameters.AddWithValue("@ID_Ingrediente", idIngrediente);
                    cmd.Parameters.AddWithValue("@QuantitàIngrediente", quantitaIngrediente);
                    cmd.Parameters.AddWithValue("@PrezzoIngrediente", prezzoIngrediente * quantitaIngrediente); 

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }


        public List<Ordine> GetOrdiniByUtente(int idUtente)
        {
            var ordini = new List<Ordine>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
        SELECT 
            o.ID_Ordine,
            o.DataOrdine,
            o.Stato,
            o.ID_Utente,  -- Aggiunto ID_Utente alla query
            r.ID_Ristorante,
            r.Nome AS NomeRistorante,
            r.Indirizzo AS IndirizzoRistorante,
            do.ID_DettaglioOrdine,
            p.ID_Piatto,
            p.Nome AS NomePiatto,
            p.Descrizione AS DescrizionePiatto,
            p.Prezzo AS PrezzoPiatto,
            do.Quantità AS QuantitàPiatto,
            p.ConsenteIngredienti,
            i.ID_Ingrediente,
            i.Nome AS NomeIngrediente,
            i.Prezzo AS PrezzoIngrediente,
            doi.QuantitàIngrediente
        FROM Ordini o
        JOIN Ristoranti r ON o.ID_Ristorante = r.ID_Ristorante
        JOIN DettagliOrdine do ON o.ID_Ordine = do.ID_Ordine
        JOIN Piatti p ON do.ID_Piatto = p.ID_Piatto
        LEFT JOIN DettagliOrdineIngredienti doi ON do.ID_DettaglioOrdine = doi.ID_DettaglioOrdine
        LEFT JOIN Ingredienti i ON doi.ID_Ingrediente = i.ID_Ingrediente
        WHERE o.ID_Utente = @ID_Utente";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Utente", idUtente);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        Ordine ordineCorrente = null;
                        DettaglioOrdine dettaglioCorrente = null;

                        while (reader.Read())
                        {
                            int idOrdine = reader.GetInt32(0);
                            int idDettaglioOrdine = reader.GetInt32(7);
                            int idPiatto = reader.GetInt32(8);

                            
                            if (ordineCorrente == null || ordineCorrente.ID_Ordine != idOrdine)
                            {
                                ordineCorrente = new Ordine
                                {
                                    ID_Ordine = idOrdine,
                                    DataOrdine = reader.GetDateTime(1),
                                    Stato = reader.GetString(2),
                                    ID_Utente = reader.GetInt32(3),  
                                    ID_Ristorante = reader.GetInt32(4),
                                    DettagliOrdine = new List<DettaglioOrdine>()
                                };
                                ordini.Add(ordineCorrente);
                            }

                            if (dettaglioCorrente == null || dettaglioCorrente.ID_DettaglioOrdine != idDettaglioOrdine)
                            {
                                dettaglioCorrente = new DettaglioOrdine
                                {
                                    ID_DettaglioOrdine = idDettaglioOrdine,
                                    ID_Ordine = idOrdine,
                                    Piatto = new Piatto
                                    {
                                        ID_Piatto = idPiatto,
                                        Nome = reader.GetString(9),
                                        Descrizione = reader.GetString(10),
                                        Prezzo = reader.GetDecimal(11)
                                    },
                                    Quantita = reader.GetInt32(12),
                                    Prezzo = reader.GetDecimal(11) * reader.GetInt32(12),
                                    Ingredienti = new List<IngredienteDettaglio>()
                                };
                                ordineCorrente.DettagliOrdine.Add(dettaglioCorrente);
                            }

                            if (!reader.IsDBNull(13))
                            {
                                var ingrediente = new IngredienteDettaglio
                                {
                                    ID_Ingrediente = reader.GetInt32(14),
                                    Nome = reader.GetString(15),
                                    Prezzo = reader.GetDecimal(16),
                                    Quantita = reader.IsDBNull(17) ? 0 : reader.GetInt32(17)
                                };
                                dettaglioCorrente.Ingredienti.Add(ingrediente);
                            }
                        }
                    }
                }
            }

            return ordini;
        }


        public async Task<List<Ingrediente>> GetAllIngredienti()
        {
            var ingredienti = new List<Ingrediente>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT ID_Ingrediente, Nome, Prezzo FROM Ingredienti";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var ingrediente = new Ingrediente
                            {
                                ID_Ingrediente = reader.GetInt32(0),
                                Nome = reader.GetString(1),
                                Prezzo = reader.GetDecimal(2)
                            };
                            ingredienti.Add(ingrediente);
                        }
                    }
                }
            }

            return ingredienti;
        }

    }
}
