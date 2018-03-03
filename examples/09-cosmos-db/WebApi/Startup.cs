﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApi.Services;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddWebApi();

            services.AddSingleton<IMyDocumentClient, MyDocumentClient>();
            services.AddSingleton<IMyDocumentClientInitializer, MyDocumentClientInitializer>();

            services.AddSingleton<IGraphQL, Services.GraphQL>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IMyDocumentClientInitializer docClientIniter)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                Task.WaitAll(docClientIniter.Reset());
            }

            app.UseMvc();            
        }
    }
}
