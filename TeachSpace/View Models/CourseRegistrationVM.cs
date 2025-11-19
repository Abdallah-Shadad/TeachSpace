using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TeachSpace.View_Models
{
    public class CourseRegistrationVM
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }

        // NEW: Store the Course Limit
        public int MaxDegree { get; set; }

        // NEW: Allow setting grade immediately
        [Range(0, int.MaxValue, ErrorMessage = "Degree must be 0 or higher")]
        public int Degree { get; set; }

        [Required(ErrorMessage = "Please select a trainee")]
        public int TraineeId { get; set; }

        public List<SelectListItem>? AvailableTrainees { get; set; }
    }
}