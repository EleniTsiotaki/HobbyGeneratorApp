public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Store hashed password
    public List<Hobby> Hobbies { get; set; } = new List<Hobby>();
    //many to many relationship
} // string.empty: default value ena empty string