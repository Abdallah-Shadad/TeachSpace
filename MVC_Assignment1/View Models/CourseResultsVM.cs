namespace MVC_Assignment1.View_Models
{
    public class CourseResultsVM
    {
        public string CourseName { get; set; }
        public int MinDegree { get; set; }

        public string DepartmentName { get; set; }
        public List<TraineeResultVM> Trainees { get; set; }
    }

}
