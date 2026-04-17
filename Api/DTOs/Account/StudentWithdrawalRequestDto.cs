using System;

namespace Api.DTOs.Account
{
    public class StudentWithdrawalRequestDto
    {
        public int Id { get; set; }
        public decimal RxCoinAmount { get; set; }
        public decimal PesoAmount { get; set; }
        public string GCashNumber { get; set; }
        public string GCashName { get; set; }
        public string Status { get; set; }
        public string AdminNotes { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ReviewedByAdminId { get; set; }
        public string ReviewedByAdminName { get; set; }
    }
}
