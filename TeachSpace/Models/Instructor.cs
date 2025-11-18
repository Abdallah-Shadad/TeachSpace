using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeachSpace.Models
{
    public class Instructor
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Imag { get; set; }
        public decimal Salary { get; set; }
        public string Address { get; set; }

        public int Dept_Id { get; set; }

        [ForeignKey("Dept_Id")]
        public Department Department { get; set; }

        public int Crs_Id { get; set; }

        [ForeignKey("Crs_Id")]
        public Course Course { get; set; }
    }
}
