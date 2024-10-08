﻿using DeliverooBackend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DeliverooBackend.Services
{
    public class UtenteService
    {
        private readonly IConfiguration _configuration;

        public UtenteService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public bool Register(string nome, string cognome, string email, string telefono, string password, string ruolo)
        {
            try
            {
                var ruoliAccettabili = new[] { "Ospite", "Ristoratore" };

                if (!ruoliAccettabili.Contains(ruolo))
                {
                    throw new ArgumentException("Ruolo non valido. Deve essere 'Ospite' o 'Ristoratore'.");
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    const string CHECK_EMAIL_CMD = @"SELECT COUNT(1) FROM Utenti WHERE Email = @Email";

                    using (SqlCommand checkEmailCmd = new SqlCommand(CHECK_EMAIL_CMD, conn))
                    {
                        checkEmailCmd.Parameters.AddWithValue("@Email", email);
                        int emailCount = (int)checkEmailCmd.ExecuteScalar();

                        if (emailCount > 0)
                        {
                            throw new Exception("Email già registrata.");
                        }
                    }
                    string passwordHash = HashPassword(password);

                    const string INSERT_CMD = @"INSERT INTO Utenti 
                                        (Nome, Cognome, Email, Telefono, PasswordHash, Ruolo)
                                        VALUES 
                                        (@Nome, @Cognome, @Email, @Telefono, @PasswordHash, @Ruolo)";

                    using (SqlCommand cmd = new SqlCommand(INSERT_CMD, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nome", nome);
                        cmd.Parameters.AddWithValue("@Cognome", cognome);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Telefono", telefono);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@Ruolo", ruolo);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante la registrazione dell'utente: " + ex.Message, ex);
            }
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }


        public Utente Login(string email, string password)
        {
            try
            {
                Console.WriteLine($"Inizio del processo di login per l'email: {email}");

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                string passwordHash = HashPassword(password);

                Console.WriteLine($"Password hash generato: {passwordHash}");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Connessione al database aperta.");
                    const string QUERY_CMD = @"SELECT * FROM Utenti WHERE Email = @Email AND PasswordHash = @PasswordHash AND Cancellato = 0";

                    using (SqlCommand cmd = new SqlCommand(QUERY_CMD, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                        Console.WriteLine("Query SQL eseguita. Inizio lettura dei dati.");

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.WriteLine("Utente trovato nel database.");

                                var user = new Utente
                                {
                                    ID_Utente = (int)reader["ID_Utente"],
                                    Nome = reader["Nome"].ToString(),
                                    Cognome = reader["Cognome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Telefono = reader["Telefono"].ToString(),
                                    Ruolo = Enum.Parse<RuoloUtente>(reader["Ruolo"].ToString())
                                };

                                Console.WriteLine("Token JWT generato.");

                                user.AccessToken = GenerateJwtToken(user);
                                user.DataScadenzaToken = DateTime.UtcNow.AddHours(24);

                                Console.WriteLine("Token aggiornato nel database.");

                                UpdateUserToken(user);

                                return user;
                            }
                            else
                            {
                                Console.WriteLine("Nessun utente trovato con le credenziali fornite o l'utente è stato cancellato.");
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il login dell'utente: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }


        private void UpdateUserToken(Utente user)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                const string UPDATE_CMD = @"UPDATE Utenti SET AccessToken = @AccessToken, DataScadenzaToken = @DataScadenzaToken WHERE ID_Utente = @ID_Utente";

                using (SqlCommand cmd = new SqlCommand(UPDATE_CMD, conn))
                {
                    cmd.Parameters.AddWithValue("@AccessToken", user.AccessToken);
                    cmd.Parameters.AddWithValue("@DataScadenzaToken", user.DataScadenzaToken);
                    cmd.Parameters.AddWithValue("@ID_Utente", user.ID_Utente);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string GenerateJwtToken(Utente user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID_Utente.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Ruolo.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public bool UpdateUser(int idUtente, string nome, string cognome, string telefono, string email, string ruolo, string password)
        {
            try
            {
                Console.WriteLine($"Aggiornamento per ID Utente: {idUtente}, Nome: {nome}, Cognome: {cognome}, Email: {email}, Telefono: {telefono}, Ruolo: {ruolo}");

                string[] ruoliAccettabili = { "Ospite", "Ristoratore", "Admin" };

               
                if (!ruoliAccettabili.Contains(ruolo))
                {
                    throw new ArgumentException("Ruolo non valido. Deve essere 'Ospite', 'Ristoratore' o 'Admin'.");
                }

                
                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("La password è obbligatoria e non può essere vuota.");
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string UPDATE_CMD = @"UPDATE Utenti 
                                  SET Nome = @Nome, Cognome = @Cognome, Telefono = @Telefono, Email = @Email, Ruolo = @Ruolo, PasswordHash = @PasswordHash
                                  WHERE ID_Utente = @ID_Utente";

                    using (SqlCommand cmd = new SqlCommand(UPDATE_CMD, conn))
                    {
                       
                        string passwordHash = HashPassword(password);

                       
                        cmd.Parameters.AddWithValue("@ID_Utente", idUtente);
                        cmd.Parameters.AddWithValue("@Nome", nome);
                        cmd.Parameters.AddWithValue("@Cognome", cognome);
                        cmd.Parameters.AddWithValue("@Telefono", telefono);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Ruolo", ruolo);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante la modifica dell'utente", ex);
            }
        }




        public Utente GetUserById(int idUtente)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                const string QUERY_CMD = @"SELECT * FROM Utenti WHERE ID_Utente = @ID_Utente AND Cancellato = 0";

                using (SqlCommand cmd = new SqlCommand(QUERY_CMD, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Utente", idUtente);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Utente
                            {
                                ID_Utente = (int)reader["ID_Utente"],
                                Nome = reader["Nome"].ToString(),
                                Cognome = reader["Cognome"].ToString(),
                                Email = reader["Email"].ToString(),
                                Telefono = reader["Telefono"].ToString(),
                                Ruolo = Enum.Parse<RuoloUtente>(reader["Ruolo"].ToString())
                            };
                        }
                    }
                }
            }
            return null;
        }




        public async Task<bool> DeleteUser(int idUtente)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string updateRistorantiQuery = @"UPDATE Ristoranti SET Cancellato = 1 WHERE ID_Utente = @ID_Utente";
                    using (SqlCommand cmdUpdateRistoranti = new SqlCommand(updateRistorantiQuery, conn))
                    {
                        cmdUpdateRistoranti.Parameters.AddWithValue("@ID_Utente", idUtente);
                        await cmdUpdateRistoranti.ExecuteNonQueryAsync();
                    }
                    string updateMenuQuery = @"UPDATE Menu SET Cancellato = 1 WHERE ID_Ristorante IN (SELECT ID_Ristorante FROM Ristoranti WHERE ID_Utente = @ID_Utente)";
                    using (SqlCommand cmdUpdateMenu = new SqlCommand(updateMenuQuery, conn))
                    {
                        cmdUpdateMenu.Parameters.AddWithValue("@ID_Utente", idUtente);
                        await cmdUpdateMenu.ExecuteNonQueryAsync();
                    }
                    string updatePiattiQuery = @"UPDATE Piatti SET Cancellato = 1 WHERE ID_Menu IN (SELECT ID_Menu FROM Menu WHERE ID_Ristorante IN (SELECT ID_Ristorante FROM Ristoranti WHERE ID_Utente = @ID_Utente))";
                    using (SqlCommand cmdUpdatePiatti = new SqlCommand(updatePiattiQuery, conn))
                    {
                        cmdUpdatePiatti.Parameters.AddWithValue("@ID_Utente", idUtente);
                        await cmdUpdatePiatti.ExecuteNonQueryAsync();
                    }
                    string updateUtenteQuery = @"UPDATE Utenti SET Cancellato = 1 WHERE ID_Utente = @ID_Utente";
                    using (SqlCommand cmdUpdateUtente = new SqlCommand(updateUtenteQuery, conn))
                    {
                        cmdUpdateUtente.Parameters.AddWithValue("@ID_Utente", idUtente);
                        int rowsAffected = await cmdUpdateUtente.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante l'eliminazione dell'utente", ex);
            }
        }
        public bool IsUserDeleted(string email)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    const string QUERY_CMD = @"SELECT COUNT(1) FROM Utenti WHERE Email = @Email AND Cancellato = 1";

                    using (SqlCommand cmd = new SqlCommand(QUERY_CMD, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);

                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante la verifica dell'utente cancellato", ex);
            }
        }


    }
}
