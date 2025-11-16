using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MVC_Assignment1.View_Models
{
    public class InstructorFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name can't be longer than 50 chars")]
        public string Name { get; set; }


        [Required]
        public decimal Salary { get; set; }

        [Required]
        public string Address { get; set; }


        public string? ExistingImage { get; set; }
        public IFormFile? UploadImage { get; set; }


        [Required(ErrorMessage = "Select a Department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Select a Course")]
        public int CourseId { get; set; }

        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Courses { get; set; } = new List<SelectListItem>();
    }
}
