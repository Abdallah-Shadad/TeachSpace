namespace TeachSpace.View_Models
{
    public class TraineeDetailsVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Image { get; set; }
        public string DepartmentName { get; set; }

        // A simple list to hold the student's academic history
        public List<TraineeCourseGradeVM> Courses { get; set; } = new();
    }

    public class TraineeCourseGradeVM
    {
        public string CourseName { get; set; }
        public int TraineeDegree { get; set; }
        public int CourseDegree { get; set; }
        public int MinDegree { get; set; }

        // Helper logic for UI
        public bool IsPassed => TraineeDegree >= MinDegree;
    }
}