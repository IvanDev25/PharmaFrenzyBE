using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class UpdateWithdrawalRequestStatusDto
    {
        [Required]
        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(500)]
        public string AdminNotes { get; set; }
    }
}
