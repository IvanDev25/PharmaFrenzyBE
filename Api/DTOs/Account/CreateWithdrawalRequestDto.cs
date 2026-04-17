using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class CreateWithdrawalRequestDto
    {
        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "RxCoin amount must be greater than zero.")]
        public decimal RxCoinAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string GCashNumber { get; set; }

        [Required]
        [MaxLength(150)]
        public string GCashName { get; set; }
    }
}
