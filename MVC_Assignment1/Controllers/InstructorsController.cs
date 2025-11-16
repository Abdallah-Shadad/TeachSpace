using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Assignment1.Models;
using MVC_Assignment1.View_Models;

namespace MVC_Assignment1.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Use Dependency Injection
        public InstructorsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var instructors = await _context.Instructors
                    .Include(i => i.Department)
                    .Include(i => i.Course)
                    .Select(i => new InstructorListVM
                    {
                        Id = i.Id,
                        Name = i.Name,
                        DepartmentName = i.Department.Name,
                        CourseName = i.Course.Name
                    })
                    .ToListAsync();


                /*
                 Query analyzation
                    Microsoft.EntityFrameworkCore.Database.Command: Information: Executed DbCommand (63ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
                    SELECT [i].[Id], [i].[Name], [d].[Name] AS [DepartmentName], [c].[Name] AS [CourseName]
                    FROM [Instructors] AS [i]
                    INNER JOIN [Departments] AS [d] ON [i].[Dept_Id] = [d].[Id]
                    INNER JOIN [Courses] AS [c] ON [i].[Crs_Id] = [c].[Id]
                 */

                // ========================================================================

                //.Select(i => new
                //{
                //    Id = i.Id,
                //    Name = i.Name,
                //    Department = i.Department.Name,
                //    Course = i.Course.Name,
                //})
                //.ToListAsync();

                /*
                 Query analyzation
                    Microsoft.EntityFrameworkCore.Database.Command: Information: Executed DbCommand (70ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
                    SELECT [i].[Id], [i].[Name], [d].[Name] AS [Department], [c].[Name] AS [Course]
                    FROM [Instructors] AS [i]
                    INNER JOIN [Departments] AS [d] ON [i].[Dept_Id] = [d].[Id]
                    INNER JOIN [Courses] AS [c] ON [i].[Crs_Id] = [c].[Id]
                 */


                // ========================================================================

                //_context.Instructors
                //.Include(i => i.Course)
                //.Include(i => i.Department)
                //.AsNoTracking()
                //.ToListAsync();

                /*
                 Query analyzation
                    Microsoft.EntityFrameworkCore.Database.Command: Information: Executed DbCommand (67ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
                    SELECT [i].[Id], [i].[Address], [i].[Crs_Id], [i].[Dept_Id], [i].[Imag], [i].[Name], [i].[Salary], [c].[Id], [c].[Degree], [c].[Dept_Id], [c].[MinDegree], [c].[Name], [d].[Id], [d].[Manager], [d].[Name]
                    FROM [Instructors] AS [i]
                    INNER JOIN [Courses] AS [c] ON [i].[Crs_Id] = [c].[Id]
                    INNER JOIN [Departments] AS [d] ON [i].[Dept_Id] = [d].[Id]
                 */
                return View(instructors);
            }
            catch (Exception ex)
            {
                // Log the error (inject ILogger in constructor for production)
                TempData["Error"] = "Error loading instructors: " + ex.Message;
                return View(new List<Instructor>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var vm = new InstructorFormVM
            {
                Departments = await _context.Departments
                    .AsNoTracking()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync(),

                Courses = await _context.Courses
                    .AsNoTracking()
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(InstructorFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                    .AsNoTracking()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync();

                vm.Courses = await _context.Courses
                    .AsNoTracking()
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();

                return View(vm);
            }

            string imageName = "default.png";

            if (vm.UploadImage != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                imageName = Guid.NewGuid() + Path.GetExtension(vm.UploadImage.FileName);
                string filePath = Path.Combine(uploadsFolder, imageName);

                using var fs = new FileStream(filePath, FileMode.Create);
                await vm.UploadImage.CopyToAsync(fs);
            }

            var instructor = new Instructor
            {
                Name = vm.Name,
                Salary = vm.Salary,
                Address = vm.Address,
                Dept_Id = vm.DepartmentId,
                Crs_Id = vm.CourseId,
                Imag = imageName
            };

            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                var instructor = await _context.Instructors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (instructor == null) return NotFound();

                var vm = new InstructorFormVM
                {
                    Id = instructor.Id,
                    Name = instructor.Name,

                    // select whole Departments

                    Departments = await _context.Departments
                        .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                        .ToListAsync(),

                    // select whole Courses
                    Courses = await _context.Courses
                        .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                        .ToListAsync(),

                    // Actual Instructor Data 
                    DepartmentId = instructor.Dept_Id,
                    CourseId = instructor.Crs_Id
                };

                return View("Edit", vm);

            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading instructor: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstructorFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                   .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                   .ToListAsync();

                vm.Courses = await _context.Courses
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();

                return View(vm);
            }
            var instructor = await _context.Instructors.FindAsync(vm.Id);
            if (instructor == null) return NotFound();

            instructor.Name = vm.Name;
            instructor.Dept_Id = vm.DepartmentId;
            instructor.Crs_Id = vm.CourseId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
    }
}
