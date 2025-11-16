using System.ComponentModel.DataAnnotations;

namespace MVC_Assignment1.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Manager { get; set; }


        // Navigation
        public List<Instructor> Instructors { get; set; }
        public List<Trainee> Trainees { get; set; }
    }
}
