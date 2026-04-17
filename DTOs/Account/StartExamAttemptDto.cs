using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class StartExamAttemptDto
    {
        [Required]
        public int SubjectId { get; set; }

        [Range(1, int.MaxValue)]
        public int QuestionSetNumber { get; set; } = 1;
    }
}
