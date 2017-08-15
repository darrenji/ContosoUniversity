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
        //�����ef�����Ĳ����̰߳�ȫ��
        private readonly SchoolContext _context;

        //ͨ�����캯������DI�е�������ע�뵽�����������
        public StudentsController(SchoolContext context)
        {
            _context = context;    
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sortOrder">��ѵ</param>
        /// <param name="searchString">���β�ѯ�ַ���</param>
        /// <param name="currentFilter">��ǰ��ѯ�ַ���</param>
        /// <param name="page">��ǰҳ</param>
        /// <returns></returns>
        // GET: Students
        //ǰ̨��ͨ��asp-route-sortOrder�����ǩ���ԣ���sortOrder������
        //searchString��ͨ�������ݹ�����
        public async Task<IActionResult> Index(string sortOrder, string searchString,string currentFilter, int? page)
        {
            //�������ҳ��ť��ʱ��CurrentSort�ͺ�����
            ViewData["CurrentSort"] = sortOrder;
            //��ҳ�滹û�е��LastName��ʱ�򣬴�ʱ�ǰ���LastName���������е�
            //û�е����ʱ��sortOrder��null, ����Ϊǰ̨׼������name_desc,Ҳ����ǰ̨�ٴε����ʱ�򣬾ͱ�ɽ�������
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            //��ҳ�滹û�е��EnrollmentDate��ʱ�򣬸���ǰ�˵���Date����ʱ�ǰ��ո�EnrollmentDate����������
            //���EnrollmentDate��ʱ�򣬸���ǰ�˵���date_desc,��ʱ����������
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if(searchString!=null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            //�������ҳ��ť��ʱ��CurrentFilter�ͺ����á���Ȼ����ѯ�ı������ʾҲ��Ҫ��
            ViewData["CurrentFilter"] = searchString;

            //�õ�����IQueryable����
            var students = from s in _context.Students
                           select s;

            //�ȹ���
            if(!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString) || s.FirstMidName.Contains(searchString));
            }

            //������
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

            //ֻ�ж����ݿ�ֱ�Ӳ����ķ����Ű����첽����Щ����������ToListAsync, SingleOrDefaultAsync, SaveChangesAsync
            //��IQueryable�����ķ�����û���첽���������磺Where
            return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(),page??1, pageSize));
        }

        // GET: Students/Details/5
        //�����id��·������������model binder��Ϊ���ǻ�ȡ·���е�idֵ
        //localhost:1230/Instructor/Index/1?courseID=2021,1����·�����ݣ�courseID������query string
        //model binderҲ���Դ�·���л�ȡid,http://localhost:1230/Instructor/Index?id=1&CourseID=2021
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //ֻ��Students��д��
            //var student = await _context.Students
            //    .SingleOrDefaultAsync(m => m.ID == id);

            //ͨ��Include��ThenInclude���԰ѹ���������ݲ�ѯ����
            var student = await _context.Students
                .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                .AsNoTracking() //�ڵ�ǰ���������У���ʹ�����Ѿ�������ʵ�壬Ҳ����ԭ�ȵ�ʵ��
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
            //�쳣�������
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
                //ModelState�����쳣������Ƶ����壬�Ѵ�����Ϣ�ŵ�������
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

        //ԭ�ȵ�д��
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("ID,LastName,FirstMidName,EnrollmentDate")] Student student)
        //{
        //    //���ж�·���е�id�Ƿ��ʵ���е�idһ��
        //    if (id != student.ID)
        //    {
        //        return NotFound();
        //    }

        //    //�����ж�ʵ���Ƿ�ͨ��
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(student);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            //�����쳣��һ�ֿ����Ǹ�ʵ��������Ѿ�����
        //            if (!StudentExists(student.ID))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        //��֤ͨ���������б�ҳ
        //        return RedirectToAction("Index");
        //    }
        //    //��֤��ͨ�������¸���
        //    return View(student);
        //}


        //����д���ĺô���ֻ����ʾ���������Բŵ��Ը���
        //�����ǣ���Ҫ��������ͻ
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var studentToUpdate = await _context.Students.SingleOrDefaultAsync(s => s.ID == id);

            //ʹ��TryUpdateModelAsync���������ڲ�����ef��change tracking�����¹���������û���������ݺ�ʵ������ݲ�һ�£��ͻ����Modified��ǩ
            //ʹ��TryUpdateModelAsync���������������ʾ��Ӳ����ķ����г���Ҫ�༭�����ݣ����ǳ��ڰ�ȫ���ǵĺܺõķ���
            //ʵ��ļ���״̬
            //Added, ��û�д��������ݿ⣬��SaveChanges��ͻ���뵽���ݿ���ȥ
            //Unchanged,�����ݿ��ж�ȡ���������ݣ��������״̬
            //Modified,ʵ���һЩ����ֵ�����˱仯��SaveChanges���ʹ�ø���������
            //Deleted,SaveChanges���ʹ��ɾ�����
            //Detached, ʵ��Ͳ����ڵ�ǰ�����Ĺ�����
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

        //���ﻹ��һ�ֱ༭���µ�д������UI���ܱ�֤ʵ���е������ֶε�ʱ�򣬿��Բ��ñ�����
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
        //        //ԭ�ȵ�д��ֻ��׽��DbUpdateConcurrencyException�쳣�����ﲻס�ĸ����쳣���㷺
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

        //�����Ƿ���ʾ������Ϣ��д��
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
            //����Ҫ��ѯ�������Ǻܶ��ʱ�����������д��������һ�ַ�������Ҫȡ������ֱ�ӱ��ɾ�����ú�savechanges�Ϳ���
            var student = await _context.Students
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);

            if(student==null)
            {
                return RedirectToAction("Index");
            }

            try
            {
                //������д��Ҳ�кô����ͻ���EF�Զ�ʵ�ּ���ɾ��
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {

                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
            
        }

        //��������ȡ������ɾ���ķ���
        //public async Task<IActionResult> DeleteConfirmed1(int id)
        //{
        //    try
        //    {
        //        Student studentToDelete = new Student() { ID = id };
        //        //ʹ�����ַ���EF���������ʵ�ּ���ɾ����ֻɾ����ǰ�ı�
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
