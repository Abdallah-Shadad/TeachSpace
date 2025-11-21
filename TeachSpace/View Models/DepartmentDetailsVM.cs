namespace TeachSpace.View_Models
{
    public class DepartmentDetailsVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Manager { get; set; }


        public List<InstructorVM> Instructors { get; set; } = new();
        public List<TraineeVM> Trainees { get; set; } = new();
    }

    public class InstructorVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CourseName { get; set; }
        public string Image { get; set; }

    }

    public class TraineeVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }

    }
}
