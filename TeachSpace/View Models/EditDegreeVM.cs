using System.ComponentModel.DataAnnotations;

namespace TeachSpace.View_Models
{
    public class EditDegreeVM
    {
        public int CourseId { get; set; }
        public int TraineeId { get; set; }
        public string? TraineeName { get; set; }
        public string? CourseName { get; set; }

        // NEW: To store the limit for the View to see
        public int MaxDegree { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Degree cannot be negative")]
        // REMOVED: [Range(0, 100)] -> We will validate the max manually in the Controller
        public int Degree { get; set; }
    }
}