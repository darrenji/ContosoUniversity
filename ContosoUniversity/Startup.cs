using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;


namespace ContosoUniversity
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        //请求管道里的配置
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //当应用程序启动起来，就会在DI容器中加入上下文
            //上下文的生命周期怎样呢？每次请求结束时，DI容器自动帮我们dispose掉当前的上下文
            //上下文怎样处理事务？当操作多张表的时候，其中一个操作没有成功，EF会帮我们自动rollback
            //上下文的tracking机制。会追踪内存中的实体和数据库中的数据是否一致，内存中的实体就是缓存。调用AsNoTracking方法可以禁用tracking机制。当然tracking机制还包括对导航属性的关联表的追踪
            //什么情况下禁用tracking机制呢？当get请求的时候，不需要ef更新实体，不需要关联导航属性，可以禁用；请求很多实体，只有少量的实体需要更新，可以禁用。
            services.AddDbContext<SchoolContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, SchoolContext context)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            DbInitializer.Initialize(context);
        }
    }
}
