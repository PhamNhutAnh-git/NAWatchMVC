using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class Message
{
    public int Id { get; set; }

    public string SenderId { get; set; } = null!;

    public string ReceiverId { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public bool? IsRead { get; set; }
}
