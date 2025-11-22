using Microsoft.AspNetCore.Mvc;

namespace TeachSpace.Helpers
{
    public static class SmartReturnHelper
    {
        public static IActionResult GoBack(
            Controller controller,
            string returnTo,
            int? deptId,
            int? courseId)
        {
            return returnTo switch
            {
                "Department" => controller.RedirectToAction("Details", "Departments", new { id = deptId }),
                "Course" => controller.RedirectToAction("Instructors", "Courses", new { id = courseId }),
                _ => controller.RedirectToAction("Index")
            };
        }
    }

}
