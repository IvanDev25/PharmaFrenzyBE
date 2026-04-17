using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class StudentWithdrawalRequest
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }
        public User Student { get; set; }

        [Required]
        public decimal RxCoinAmount { get; set; }

        [Required]
        public decimal PesoAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string GCashNumber { get; set; }

        [Required]
        [MaxLength(150)]
        public string GCashName { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string AdminNotes { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAtUtc { get; set; }

        public string ReviewedByAdminId { get; set; }
        public User ReviewedByAdmin { get; set; }
    }
}
