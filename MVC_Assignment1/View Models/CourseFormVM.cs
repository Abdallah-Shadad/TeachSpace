using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MVC_Assignment1.View_Models
{
    public class CourseFormVM : IValidatableObject
    {
        public int? Id { get; set; }   // null when Add, value when Edit

        [Required(ErrorMessage = "Course name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Course Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Degree is required")]
        [Range(0, 100, ErrorMessage = "Degree must be between 0 and 100")]
        [Display(Name = "Maximum Degree")]
        public int Degree { get; set; }

        [Required(ErrorMessage = "Minimum degree is required")]
        [Range(0, 100, ErrorMessage = "Minimum degree must be between 0 and 100")]
        [Display(Name = "Minimum Passing Degree")]
        public int MinDegree { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a department")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        // For dropdown list
        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();

        // Custom validation to ensure MinDegree <= Degree
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinDegree > Degree)
            {
                yield return new ValidationResult(
                    "Minimum passing degree cannot be greater than maximum degree",
                    new[] { nameof(MinDegree) });
            }
        }
    }
}