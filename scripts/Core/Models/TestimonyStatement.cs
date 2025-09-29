using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class TestimonyStatement
    {
        [Required]
        public string Id { get; set; } = string.Empty;
        [Required]
        public string SuspectId { get; set; } = string.Empty;
        [Required]
        public string OriginalText { get; set; } = string.Empty;

        public string? AmendedText { get; set; }
        public bool IsAmended => !string.IsNullOrEmpty(AmendedText);

        public string CurrentText => IsAmended ? AmendedText! : OriginalText;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();

        public T GetMetadata<T>(string key) where T : class =>
            Metadata.TryGetValue(key, out var value) ? value as T : null;
    }
}
