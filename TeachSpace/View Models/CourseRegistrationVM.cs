using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TeachSpace.View_Models
{
    public class CourseRegistrationVM
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; } // Optional: to display on screen

        [Required(ErrorMessage = "Please select a trainee")]
        public int TraineeId { get; set; }

        [Range(0, 100)]
        public int Degree { get; set; }

        // This list will hold only Trainees NOT yet registered in this course
        public List<SelectListItem>? AvailableTrainees { get; set; }

    }
}