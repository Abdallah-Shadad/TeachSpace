using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeachSpace.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Degree { get; set; }
        public int MinDegree { get; set; }

        public int Dept_Id { get; set; }

        [ForeignKey("Dept_Id")]
        public Department Department { get; set; }


        // One Course → Many Instructors
        // One Instructor -> One Course
        public List<Instructor> Instructors { get; set; }

        // Many-to-Many via CrsResult
        public List<CrsResult> CrsResults { get; set; }
    }
}
