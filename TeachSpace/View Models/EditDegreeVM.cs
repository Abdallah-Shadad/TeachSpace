using System.ComponentModel.DataAnnotations;

namespace TeachSpace.View_Models
{
    public class EditDegreeVM
    {
        public int CourseId { get; set; }
        public int TraineeId { get; set; }

        public string? TraineeName { get; set; }
        public string? CourseName { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Degree must be between 0 and 100")]
        public int Degree { get; set; }
    }
}