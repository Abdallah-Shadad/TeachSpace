using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TeachSpace.View_Models
{
    public class DepartmentFormVM
    {
        // Vital for excluding the current record during Edit validation
        public int Id { get; set; }

        [Required(ErrorMessage = "Department Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Department Name")]
        // Remote Validation: Checks with the server instantly while typing
        [Remote(action: "CheckName", controller: "Departments", AdditionalFields = "Id", ErrorMessage = "This Department Name already exists!")]
        public string Name { get; set; }

        [Display(Name = "Manager Name")]
        public string Manager { get; set; }
    }
}