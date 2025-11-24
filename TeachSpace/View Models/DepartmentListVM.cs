using TeachSpace.Attributes;
using TeachSpace.Models;

namespace TeachSpace.View_Models
{
    public class DepartmentListVM
    {
        public int? Id { get; set; }

        [Unique(typeof(Department))]
        public string Name { get; set; }
        public string Manager { get; set; }
    }
}
