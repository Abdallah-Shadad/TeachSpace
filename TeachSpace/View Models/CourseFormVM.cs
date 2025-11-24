using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TeachSpace.Attributes;
using TeachSpace.Models;

namespace TeachSpace.View_Models
{
    public class CourseFormVM : IValidatableObject
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Course name is required")]
        [StringLength(100)]
        [Display(Name = "Course Name")]
        [Unique(typeof(Course))]
        [Remote(action: "CheckName", controller: "Courses", AdditionalFields = "Id", ErrorMessage = "Course Name Is Already Exist!")]
        public string Name { get; set; }

        [Required]
        [Range(0, 100)]
        public int Degree { get; set; }

        [Required]
        [Range(0, 100)]
        public int MinDegree { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinDegree > Degree)
            {
                yield return new ValidationResult("Min Degree cannot be > Max Degree", new[] { nameof(MinDegree) });
            }
        }
    }
}