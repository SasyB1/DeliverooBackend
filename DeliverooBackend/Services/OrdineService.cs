﻿using DeliverooBackend.Models;
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
            o.ID_Utente,  
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


                                    NomeRistorante = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,

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
                                    ID_Ingrediente = !reader.IsDBNull(14) ? reader.GetInt32(14) : 0,
                                    Nome = !reader.IsDBNull(15) ? reader.GetString(15) : string.Empty,
                                    Prezzo = !reader.IsDBNull(16) ? reader.GetDecimal(16) : 0,
                                    Quantita = !reader.IsDBNull(17) ? reader.GetInt32(17) : 0
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

        public async Task AggiungiRecensione(int idOrdine, int idRistorante, int idUtente, int valutazione, string commento)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
            INSERT INTO RecensioniRistoranti (ID_Ordine, ID_Ristorante, ID_Utente, Valutazione, Commento, DataRecensione)
            VALUES (@ID_Ordine, @ID_Ristorante, @ID_Utente, @Valutazione, @Commento, @DataRecensione)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Ordine", idOrdine);
                    cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);
                    cmd.Parameters.AddWithValue("@ID_Utente", idUtente);
                    cmd.Parameters.AddWithValue("@Valutazione", valutazione);
                    cmd.Parameters.AddWithValue("@Commento", string.IsNullOrEmpty(commento) ? DBNull.Value : (object)commento);
                    cmd.Parameters.AddWithValue("@DataRecensione", DateTime.Now);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<List<Recensione>> GetRecensioniByRistorante(int idRistorante)
        {
            var recensioni = new List<Recensione>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
            SELECT 
                r.ID_Recensione,
                r.ID_Utente,
                u.Nome AS NomeUtente,
                r.Valutazione,
                r.Commento,
                r.DataRecensione
            FROM RecensioniRistoranti r
            JOIN Utenti u ON r.ID_Utente = u.ID_Utente
            WHERE r.ID_Ristorante = @ID_Ristorante";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var recensione = new Recensione
                            {
                                ID_Recensione = reader.GetInt32(0),
                                ID_Utente = reader.GetInt32(1),
                                NomeUtente = reader.GetString(2),
                                Valutazione = reader.GetInt32(3),
                                Commento = reader.IsDBNull(4) ? null : reader.GetString(4),
                                DataRecensione = reader.GetDateTime(5)
                            };
                            recensioni.Add(recensione);
                        }
                    }
                }
            }

            return recensioni;
        }
        public async Task<List<Ordine>> GetOrdiniByRistorante(int idRistorante)
        {
            var ordini = new List<Ordine>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
        SELECT 
            o.ID_Ordine,
            o.DataOrdine,
            o.Stato,
            o.ID_Utente,  
            o.ID_Ristorante,  
            r.Nome AS NomeRistorante,
            do.ID_DettaglioOrdine,
            p.Nome AS NomePiatto,
            p.Prezzo AS PrezzoPiatto,
            do.Quantità AS QuantitàPiatto
        FROM Ordini o
        JOIN Ristoranti r ON o.ID_Ristorante = r.ID_Ristorante
        JOIN DettagliOrdine do ON o.ID_Ordine = do.ID_Ordine
        JOIN Piatti p ON do.ID_Piatto = p.ID_Piatto
        WHERE o.ID_Ristorante = @ID_Ristorante";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Ristorante", idRistorante);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Ordine ordineCorrente = null;

                        while (await reader.ReadAsync())
                        {
                            int idOrdine = reader.GetInt32(0);
                            if (ordineCorrente == null || ordineCorrente.ID_Ordine != idOrdine)
                            {
                                ordineCorrente = new Ordine
                                {
                                    ID_Ordine = idOrdine,
                                    DataOrdine = reader.GetDateTime(1),
                                    Stato = reader.GetString(2),
                                    ID_Utente = reader.GetInt32(3),
                                    ID_Ristorante = reader.GetInt32(4),
                                    NomeRistorante = reader.GetString(5),
                                    DettagliOrdine = new List<DettaglioOrdine>()
                                };
                                ordini.Add(ordineCorrente);
                            }

                            var dettaglioOrdine = new DettaglioOrdine
                            {
                                ID_DettaglioOrdine = reader.GetInt32(6),
                                Piatto = new Piatto
                                {
                                    Nome = reader.GetString(7),
                                    Prezzo = reader.GetDecimal(8)
                                },
                                Quantita = reader.GetInt32(9)
                            };

                            ordineCorrente.DettagliOrdine.Add(dettaglioOrdine);
                        }
                    }
                }
            }

            return ordini;
        }

        public async Task CambiaStatoOrdine(int idOrdine, string nuovoStato)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
        UPDATE Ordini
        SET Stato = @NuovoStato
        WHERE ID_Ordine = @ID_Ordine";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Ordine", idOrdine);
                    cmd.Parameters.AddWithValue("@NuovoStato", nuovoStato);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Ordine non trovato o nessun aggiornamento applicato.");
                    }
                }
            }
        }

    }
}
