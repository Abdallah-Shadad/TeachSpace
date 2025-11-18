using X.PagedList;

namespace TeachSpace.View_Models
{
    public class CourseResultsVM
    {
        public string CourseName { get; set; }
        public int MinDegree { get; set; }
        public string DepartmentName { get; set; }

        public IPagedList<TraineeResultVM> Trainees { get; set; }
    }
}