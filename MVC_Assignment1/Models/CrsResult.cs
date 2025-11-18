using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeachSpace.Models
{
    public class CrsResult
    {
        [Key]
        public int Id { get; set; }

        public int Degree { get; set; }

        // Foreign Key for Course
        public int Crs_Id { get; set; }

        [ForeignKey("Crs_Id")]
        public Course Course { get; set; }

        // Foreign Key for Trainee
        public int Trainee_Id { get; set; }

        [ForeignKey("Trainee_Id")]
        public Trainee Trainee { get; set; }
    }
}
