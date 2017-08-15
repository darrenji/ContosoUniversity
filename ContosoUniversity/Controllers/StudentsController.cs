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
    public class StudentsController : Controller
    {
        //这里的ef上下文不是线程安全的
        private readonly SchoolContext _context;

        //通过构造函数，把DI中的上下文注入到这个控制器来
        public StudentsController(SchoolContext context)
        {
            _context = context;    
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sortOrder">培训</param>
        /// <param name="searchString">本次查询字符串</param>
        /// <param name="currentFilter">当前查询字符串</param>
        /// <param name="page">当前页</param>
        /// <returns></returns>
        // GET: Students
        //前台是通过asp-route-sortOrder这个标签属性，把sortOrder传进来
        //searchString是通过表单传递过来的
        public async Task<IActionResult> Index(string sortOrder, string searchString,string currentFilter, int? page)
        {
            //当点击分页按钮的时候，CurrentSort就很有用
            ViewData["CurrentSort"] = sortOrder;
            //当页面还没有点击LastName的时候，此时是按照LastName的升序排列的
            //没有点击的时候，sortOrder是null, 这里为前台准备的是name_desc,也就是前台再次点击的时候，就变成降序排列
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            //当页面还没有点击EnrollmentDate的时候，给到前端的是Date，此时是按照该EnrollmentDate的升序排列
            //点击EnrollmentDate的时候，给到前端的是date_desc,此时按降序排列
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if(searchString!=null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            //当点击分页按钮的时候，CurrentFilter就很有用。当然，查询文本款的显示也需要它
            ViewData["CurrentFilter"] = searchString;

            //得到的是IQueryable集合
            var students = from s in _context.Students
                           select s;

            //先过滤
            if(!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString) || s.FirstMidName.Contains(searchString));
            }

            //再排序
            switch(sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.LastName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.EnrollmentDate);
                    break;
                case "date_desc":
                    students = students.OrderByDescending(s => s.EnrollmentDate);
                    break;
                default:
                    students = students.OrderBy(s => s.LastName);
                    break;
            }

            int pageSize = 3;

            //只有对数据库直接操作的方法才包含异步，这些方法包括：ToListAsync, SingleOrDefaultAsync, SaveChangesAsync
            //对IQueryable操作的方法，没有异步方法，比如：Where
            return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(),page??1, pageSize));
        }

        // GET: Students/Details/5
        //这里的id从路由数据中来，model binder会为我们获取路由中的id值
        //localhost:1230/Instructor/Index/1?courseID=2021,1就是路由数据，courseID就是是query string
        //model binder也可以从路由中获取id,http://localhost:1230/Instructor/Index?id=1&CourseID=2021
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //只是Students的写法
            //var student = await _context.Students
            //    .SingleOrDefaultAsync(m => m.ID == id);

            //通过Include和ThenInclude可以把关联表的数据查询出来
            var student = await _context.Students
                .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                .AsNoTracking() //在当前的上下文中，即使有人已经更新了实体，也还是原先的实体
                .SingleOrDefaultAsync(m => m.ID == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //Bind attribute is one way to protect against overposting in create secarios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,EnrollmentDate")] Student student)
        {
            //异常捕获机制
            try
            {
                if(ModelState.IsValid)
                {
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }   
            }
            catch (DbUpdateException)
            {
                //ModelState就是异常捕获机制的载体，把错误信息放到这里来
                ModelState.AddModelError("", "Unable to save changes. " + "Try again, and if the problem persists " + "see your system adminstrator");
            }
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.SingleOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        //原先的写法
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("ID,LastName,FirstMidName,EnrollmentDate")] Student student)
        //{
        //    //先判断路由中的id是否和实体中的id一致
        //    if (id != student.ID)
        //    {
        //        return NotFound();
        //    }

        //    //再来判断实体是否通过
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(student);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            //更新异常的一种可能是该实体和数据已经存在
        //            if (!StudentExists(student.ID))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        //验证通过就来到列表页
        //        return RedirectToAction("Index");
        //    }
        //    //验证不通过就重新更新
        //    return View(student);
        //}


        //这种写法的好处：只有显示声明的属性才得以更新
        //坏处是：需要处理并发冲突
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var studentToUpdate = await _context.Students.SingleOrDefaultAsync(s => s.ID == id);

            //使用TryUpdateModelAsync方法，其内部会让ef的change tracking做如下工作：如果用户输入的数据和实体的数据不一致，就会打上Modified标签
            //使用TryUpdateModelAsync方法，其参数中显示、硬编码的方法列出需要编辑的数据，这是出于安全考虑的很好的方法
            //实体的几种状态
            //Added, 还没有存在于数据库，当SaveChanges后就会插入到数据库中去
            //Unchanged,从数据库中读取出来的数据，就是这个状态
            //Modified,实体的一些属性值发生了变化，SaveChanges后就使用更新语句更新
            //Deleted,SaveChanges后就使用删除语句
            //Detached, 实体就不属于当前上下文管理了
            if(await TryUpdateModelAsync<Student>(studentToUpdate,"", s => s.FirstMidName, s => s.LastName, s => s.EnrollmentDate))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {

                    ModelState.AddModelError("", "unable to save changes");
                }
            }
            return View(studentToUpdate);
        }

        //这里还有一种编辑更新的写法，当UI中能保证实体中的所有字段的时候，可以采用本方法
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("ID,EnrollmentDate,FirstMidName,LastName")]Student student)
        //{
        //    if(id != student.ID)
        //    {
        //        return NotFound();
        //    }

        //    if(ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(student);
        //            await _context.SaveChangesAsync();
        //            return RedirectToAction("Index");
        //        }
        //        //原先的写法只捕捉了DbUpdateConcurrencyException异常，这里不住的更新异常更广泛
        //        catch (DbUpdateException)
        //        {

        //            ModelState.AddModelError("", "unable to save changes");
        //        }
        //    }
        //    return View(student);
        //}

        // GET: Students/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var student = await _context.Students
        //        .SingleOrDefaultAsync(m => m.ID == id);
        //    if (student == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(student);
        //}

        //带上是否显示出错信息的写法
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if(id==null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);
            
            if(student == null)
            {
                return NotFound();
            }

            if(saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] = "Delete failed";
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //这里要查询，请求不是很多的时候可以用这种写法，还有一种方法不需要取出来，直接标记删除，让后savechanges就可以
            var student = await _context.Students
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);

            if(student==null)
            {
                return RedirectToAction("Index");
            }

            try
            {
                //这样的写法也有好处，就会让EF自动实现级联删除
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {

                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
            
        }

        //不把数据取出来再删除的方法
        //public async Task<IActionResult> DeleteConfirmed1(int id)
        //{
        //    try
        //    {
        //        Student studentToDelete = new Student() { ID = id };
        //        //使用这种方法EF不会帮我们实现级联删除，只删除当前的表
        //        _context.Entry(studentToDelete).State = EntityState.Deleted;
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    catch (DbUpdateException)
        //    {

        //        return RedirectToAction("Delete", new { id = id, saveChangesError = true });
        //    }
        //}

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.ID == id);
        }
    }
}
