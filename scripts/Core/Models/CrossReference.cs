using System;
using System.ComponentModel.DataAnnotations;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class CrossReference
    {
        [Required]
        public string FromSuspectId { get; set; } = string.Empty;
        [Required]
        public string ToSuspectId { get; set; } = string.Empty;
        [Required]
        public string RelationshipType { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;
        public DateTime EstablishedAt { get; set; } = DateTime.UtcNow;
    }
}
