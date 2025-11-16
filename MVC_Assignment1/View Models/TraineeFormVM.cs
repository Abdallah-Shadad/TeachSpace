using Microsoft.AspNetCore.Mvc.Rendering;
using MVC_Assignment1.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MVC_Assignment1.View_Models
{
    public class TraineeFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Select a department")]
        public int Dept_Id { get; set; }

        public string? ExistingImage { get; set; }
        public IFormFile? UploadImage { get; set; }

        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
    }

}
