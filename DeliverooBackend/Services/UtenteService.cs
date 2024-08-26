using Microsoft.Data.SqlClient;
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
        public bool Register(string nome, string cognome, string email, string telefono, string indirizzo, string password, string ruolo)
        {
            try
            {
                // Recupero della stringa di connessione dal file di configurazione
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                // Hash della password
                string passwordHash = HashPassword(password);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    const string INSERT_CMD = @"INSERT INTO Utenti 
                                                (Nome, Cognome, Email, Telefono, Indirizzo, PasswordHash, Ruolo)
                                                VALUES 
                                                (@Nome, @Cognome, @Email, @Telefono, @Indirizzo, @PasswordHash, @Ruolo)";

                    using (SqlCommand cmd = new SqlCommand(INSERT_CMD, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nome", nome);
                        cmd.Parameters.AddWithValue("@Cognome", cognome);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Telefono", telefono);
                        cmd.Parameters.AddWithValue("@Indirizzo", indirizzo);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@Ruolo", ruolo);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante la registrazione dell'utente", ex);
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

    }
}
