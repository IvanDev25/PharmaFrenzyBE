using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class SubmitExamAnswerDto
    {
        [Required]
        public int QuestionId { get; set; }

        [MaxLength(1)]
        public string SelectedAnswer { get; set; }

        [Range(0, int.MaxValue)]
        public int TimeSpentSeconds { get; set; }
    }
}
