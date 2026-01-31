using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TeachSpace.View_Models
{
    public class CourseRegistrationVM
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }

        // Email mode
        [EmailAddress]
        public string? TraineeEmail { get; set; }

        // Select mode
        public int? TraineeId { get; set; }
        public List<SelectListItem>? AvailableTrainees { get; set; }

        // Grade
        public int MaxDegree { get; set; }

        [Range(0, int.MaxValue)]
        public int Degree { get; set; }
    }

}
