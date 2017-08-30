using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly SchoolContext _context;

        public DepartmentsController(SchoolContext context)
        {
            _context = context;    
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            var schoolContext = _context.Departments.Include(d => d.Administrator);
            return View(await schoolContext.ToListAsync());
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Administrator)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.DepartmentID == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            ViewData["InstructorID"] = new SelectList(_context.Instructors, "ID", "FullName");
            return View();
        }

        // POST: Departments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentID,Name,Budget,StartDate,InstructorID,RowVersion")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewData["InstructorID"] = new SelectList(_context.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(i=>i.Administrator)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.DepartmentID == id);
            if (department == null)
            {
                return NotFound();
            }
            ViewData["InstructorID"] = new SelectList(_context.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // POST: Departments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //页面中的rowVersion是放在隐藏域中传递过来的
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, byte[] rowVersion)
        {
            if(id==null)
            {
                return NotFound();
            }

            var departmentToUpdate = await _context.Departments
                .Include(i => i.Administrator)
                .SingleOrDefaultAsync(m => m.DepartmentID == id);

            if(departmentToUpdate==null)
            {
                Department deletedDepartment = new Department();
                await TryUpdateModelAsync(deletedDepartment);
                ModelState.AddModelError(string.Empty, "unable to save changes.The department was deleted by another user.");
                ViewData["InstructorID"] = new SelectList(_context.Instructors, "ID", "FullName", deletedDepartment.InstructorID);
                return View(deletedDepartment);
            }

            //原始值就是页面中传来的值
            _context.Entry(departmentToUpdate).Property("RowVersion").OriginalValue = rowVersion;

            //TryUpdateModelAsyn会执行update的sql语句，where带的条件包含RowVersion,如果找不到需要更新的row,就会抛异常了
            if(await TryUpdateModelAsync<Department>(departmentToUpdate,"",s=>s.Name,s=>s.StartDate,s=>s.Budget,s=>s.InstructorID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)//如果找不到需要更新的row，意味着已经有人修改这条数据了，就要抛异常
                {
                    //有很多Entry，获取一个
                    //这个entry可以获取客户端输入的值和数据库的值
                    var exceptionEntry = ex.Entries.Single();

                    //从Entry中获取客户端的实体
                    var clientValues = (Department)exceptionEntry.Entity;
                    //从Entry中获取数据库端实体
                    var databaseEntry = exceptionEntry.GetDatabaseValues();

                    if(databaseEntry==null)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to save changes.The department was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Department)databaseEntry.ToObject();
                        if(databaseValues.Name!=clientValues.Name)
                        {
                            ModelState.AddModelError("Name", $"Current value:{databaseValues.Name}");
                        }
                        if(databaseValues.Budget!=clientValues.Budget)
                        {
                            ModelState.AddModelError("Budget", $"Currentvalue:{databaseValues.Budget:c}");
                        }
                        if(databaseValues.StartDate!=clientValues.StartDate)
                        {
                            ModelState.AddModelError("StartDate", $"Current value:{databaseValues.StartDate:d}");
                        }
                        if(databaseValues.InstructorID!=clientValues.InstructorID)
                        {
                            Instructor databaseInstructor = await _context.Instructors.SingleOrDefaultAsync(i => i.ID == databaseValues.InstructorID);
                            ModelState.AddModelError("InstructorID", $"Current value:{databaseInstructor?.FullName}");
                        }
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you got the orginal value. The "
                            + "edit operation was canceled and the current values in the database "
                            + "have been displayed.If you still want to edit this record, click "
                            + "the Save button again.Otherwise click the back to lIst hyperlink.");

                        //把当前数据库的RowVersion再返回到页面
                        departmentToUpdate.RowVersion = (byte[])databaseValues.RowVersion;
                        //ModelState中的RowVersion还是原来的值，也需要去掉
                        ModelState.Remove("RowVersion");
                    }
                }
            }
            ViewData["InstructorID"] = new SelectList(_context.Instructors, "ID", "FullName", departmentToUpdate.InstructorID);
            return View(departmentToUpdate);
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? concurrencyError)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Administrator)
                .SingleOrDefaultAsync(m => m.DepartmentID == id);
            if (department == null)
            {
                if(concurrencyError.GetValueOrDefault())
                {
                    return RedirectToAction(nameof(Index));
                }
                return NotFound();
            }

            if(concurrencyError.GetValueOrDefault())
            {
                    ViewData["ConcurrencyErrorMessage"] = "The record you attempted to delete "
                + "was modified by another user after you got the original values. "
                + "The delete operation was canceled and the current values in the "
                + "database have been displayed. If you still want to delete this "
                + "record, click the Delete button again. Otherwise "
                + "click the Back to List hyperlink.";
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Department department)
        {
            try
            {
                if(await _context.Departments.AnyAsync(m=>m.DepartmentID==department.DepartmentID))
                {
                    _context.Departments.Remove(department);
                    await _context.SaveChangesAsync();
                }
                //如果确实删除了
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {

                return RedirectToAction(nameof(Delete), new { concurrencyError = true, id = department.DepartmentID });
            }
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentID == id);
        }
    }
}
