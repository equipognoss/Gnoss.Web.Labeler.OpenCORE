using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Util.Seguridad;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.OpenReplication;
using Es.Riam.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gnoss.Web.Labeler
{
    public class Startup
    {
        public Startup(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            Configuration = configuration;
            mEnvironment = environment;
        }

        public IConfiguration Configuration { get; }
        public Microsoft.AspNetCore.Hosting.IHostingEnvironment mEnvironment { get; }
        private string IdiomaPrincipalDominio = "es";
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            bool cargado = false;

            services.AddCors(options =>
            {
                options.AddPolicy(name: "_myAllowSpecificOrigins",
                                  builder =>
                                  {
                                      builder.AllowAnyOrigin();
                                      builder.AllowAnyMethod();
                                  });
            });

            services.AddControllers();
            services.AddHttpContextAccessor();
            services.AddScoped(typeof(UtilTelemetry));
            services.AddScoped(typeof(Usuario));
            services.AddScoped(typeof(UtilPeticion));
            services.AddScoped(typeof(Conexion));
            services.AddScoped(typeof(UtilGeneral));
            services.AddScoped(typeof(LoggingService));
            services.AddScoped(typeof(RedisCacheWrapper));
            services.AddScoped(typeof(Configuracion));
            services.AddScoped(typeof(GnossCache));
            services.AddScoped(typeof(VirtuosoAD));
            //services.AddScoped(typeof(UtilServicios));
            services.AddScoped<IServicesUtilVirtuosoAndReplication, ServicesVirtuosoAndBidirectionalReplicationOpen>();
            string bdType = "";
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            if (environmentVariables.Contains("connectionType"))
            {
                bdType = environmentVariables["connectionType"] as string;
            }
            else
            {
                bdType = Configuration.GetConnectionString("connectionType");
            }
            if (bdType.Equals("2"))
            {
                services.AddScoped(typeof(DbContextOptions<EntityContext>));
                services.AddScoped(typeof(DbContextOptions<EntityContextBASE>));
            }
            services.AddSingleton(typeof(ConfigService));
            services.AddSingleton<ILoggerFactory, LoggerFactory>();

            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // Tiempo de expiración   
                                                                //options.Cookie.Name = "AppTest";
                                                                //options.Cookie.HttpOnly = true; // correct initialization

            });
            services.AddMvc();

            string acid = "";
            if (environmentVariables.Contains("acid"))
            {
                acid = environmentVariables["acid"] as string;
            }
            else
            {
                acid = Configuration.GetConnectionString("acid");
            }
            string baseConnection = "";
            if (environmentVariables.Contains("base"))
            {
                baseConnection = environmentVariables["base"] as string;
            }
            else
            {
                baseConnection = Configuration.GetConnectionString("base");
            }
            if (bdType.Equals("0"))
            {
                services.AddDbContext<EntityContext>(options =>
                        options.UseSqlServer(acid)
                        );
                services.AddDbContext<EntityContextBASE>(options =>
                        options.UseSqlServer(baseConnection)

                        );
            }
            else if (bdType.Equals("2"))
            {
                services.AddDbContext<EntityContext, EntityContextPostgres>(opt =>
                {
                    var builder = new NpgsqlDbContextOptionsBuilder(opt);
                    builder.SetPostgresVersion(new Version(9, 6));
                    opt.UseNpgsql(acid);

                });
                services.AddDbContext<EntityContextBASE, EntityContextBASEPostgres>(opt =>
                {
                    var builder = new NpgsqlDbContextOptionsBuilder(opt);
                    builder.SetPostgresVersion(new Version(9, 6));
                    opt.UseNpgsql(baseConnection);

                });
            }
            var sp = services.BuildServiceProvider();
            // Resolve the services from the service provider
            var loggingService = sp.GetService<LoggingService>();
            UtilTelemetry utilTelemetry = sp.GetService<UtilTelemetry>();
            loggingService.AgregarEntrada("INICIO Application_Start");
            LoggingService.RUTA_DIRECTORIO_ERROR = Path.Combine(mEnvironment.ContentRootPath, "logs");
            loggingService.GuardarLogError("Application_Start");
            // Resolve the services from the service provider

            var entity = sp.GetService<EntityContext>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gnoss.Web.ServiceAutocompleteTags", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Gnoss.Web.Labeler v1"));

            app.UseRouting();
            app.UseCors();
            app.UseSession();
            app.UseAuthorization();
 
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        
    }
}
