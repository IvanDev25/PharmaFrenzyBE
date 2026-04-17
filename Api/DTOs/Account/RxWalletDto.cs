using System.Collections.Generic;

namespace Api.DTOs.Account
{
    public class RxWalletDto
    {
        public decimal AvailableRxCoinBalance { get; set; }
        public decimal PendingRxCoinBalance { get; set; }
        public decimal TotalRxCoinBalance { get; set; }
        public decimal AvailablePesoEquivalent { get; set; }
        public decimal PendingPesoEquivalent { get; set; }
        public decimal ConversionRateRxCoinPerPeso { get; set; }
        public List<StudentWithdrawalRequestDto> WithdrawalRequests { get; set; } = new List<StudentWithdrawalRequestDto>();
    }
}
