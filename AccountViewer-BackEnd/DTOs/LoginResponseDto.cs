namespace AccountsViewer.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
    }
}
