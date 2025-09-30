using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class Evidence
    {
        [Required]
        public string Id { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;

        public Dictionary<string, object> Content { get; set; } = new();
        public Dictionary<string, object> UiDisplay { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string GetContentValue(string key) =>
            Content.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

        public T GetContentValue<T>(string key) where T : class =>
            Content.TryGetValue(key, out var value) ? value as T : null;
    }
}
