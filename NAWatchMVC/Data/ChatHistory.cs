using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.Data
{
    public class ChatHistory
    {
        [Key]
        public int Id { get; set; }

        public string? CustomerId { get; set; }

        [Required]
        public string UserQuestion { get; set; }

        public string? AiResponse { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? SessionId { get; set; }
    }
}