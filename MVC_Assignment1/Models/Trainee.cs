using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeachSpace.Models
{
    public class Trainee
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Imag { get; set; } = "default.png"; // default image
        public string Address { get; set; }

        [Required(ErrorMessage = "Please select a department")]
        public int Dept_Id { get; set; }

        [ForeignKey("Dept_Id")]
        [ValidateNever]
        public Department Department { get; set; }

        // One Trainee → Many Course Results

        [ValidateNever]
        public List<CrsResult> CrsResults { get; set; }
    }
}
