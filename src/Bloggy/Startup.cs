﻿using Bloggy.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Bloggy
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Environment = env;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.ContentRootPath)
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        public IHostingEnvironment Environment { get; set; }

        public BloggingContext Db { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            if (Environment.IsDevelopment())
            {
                services.AddDbContext<BloggingContext>(options => options.UseInMemoryDatabase("bloggy"));
            }
            else
            {
                services.AddDbContext<BloggingContext>(options => options.UseSqlite(Configuration["Data:DefaultConnection:ConnectionString"]));
            }

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.Configure<AppSettings>(options => Configuration.GetSection("AppSettings").Bind(options));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie();

            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeFolder("/Account");
                    options.Conventions.AllowAnonymousToPage("/Account/Login");
                });

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new ThemeViewLocationExpander());
            });

            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, BloggingContext db)
        {
            if (Environment.IsDevelopment())
            {
                Db = db;
                loggerFactory.AddConsole(Configuration.GetSection("Logging"));
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                AddSeedData();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseAuthentication();

            app.UseResponseCompression();

            app.UseStaticFiles();

            var appSettings = app.ApplicationServices.GetRequiredService<IOptions<AppSettings>>();

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(),
                    $@"Pages/Themes/{appSettings.Value.Blog.Theme}")),
                RequestPath = string.Empty
            });

            app.UseMvcWithDefaultRoute();
        }

        private void AddSeedData()
        {
            for (int i = 1; i <= 10; i++)
            {
                var post = new Post
                {
                    Title = "What is Lorem Ipsum?",
                    Slug = "what-is-lorem-ipsum-" + i,
                    Excerpt = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s",
                    Content = @"**Lorem Ipsum** is simply dummy text of the printing and typesetting industry. **Lorem Ipsum** has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing **Lorem Ipsum** passages, and more recently with desktop publishing software like Aldus PageMaker including versions of **Lorem Ipsum**.",
                    IsPublished = true,
                    PublishedAt = new DateTime(2016, 12, 2),
                    Tags = "Lorem Ipsum,lorem,ipsum"
                };
                post.Comments = new List<Comment>();

                for (int j = 1; j < 3; j++)
                {
                    var comment = new Comment()
                    {
                        Author = "Lorem Ipsum",
                        Email = "lorem@ipsum.com",
                        Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry.",
                        Website = "http://www.lipsum.com",
                        PublishedAt = new DateTime(2016, 12, 14),
                        PostId = i,
                        Post = post
                    };
                    post.Comments.Add(comment);
                }
                Db.Posts.Add(post);
            }

            Db.SaveChanges();
        }
    }
}
