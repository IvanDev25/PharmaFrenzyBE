using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class SubmitExamAttemptDto
    {
        [Required]
        public List<SubmitExamAnswerDto> Answers { get; set; } = new List<SubmitExamAnswerDto>();
    }
}
