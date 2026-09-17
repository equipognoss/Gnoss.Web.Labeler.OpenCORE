using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.RelatedVirtuoso;
using Es.Riam.Gnoss.HealthChecks;
using Es.Riam.Gnoss.RabbitMQ;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Util.Seguridad;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Interfaces.InterfacesOpen;
using Es.Riam.InterfacesOpen;
using Es.Riam.Open;
using Es.Riam.OpenReplication;
using Es.Riam.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using ServicioAutoCompletarMVC;
using System;
using System.Collections;
using System.IO;

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
            // Provider temporal solo para el logger de arranque
            using var tempProvider = services.BuildServiceProvider();
            var logger = tempProvider.GetRequiredService<ILogger<Startup>>();

            services.AddCors(options =>
            {
                options.AddPolicy(name: "_myAllowSpecificOrigins",
                                  builder =>
                                  {
									  builder.SetIsOriginAllowed(UtilServicios.ComprobarDominioPermitidoCORS);
									  builder.AllowAnyHeader();
									  builder.AllowAnyMethod();
									  builder.AllowCredentials();
								  });
            });

            services.AddControllers();
            services.AddHttpContextAccessor();
            services.AddScoped(typeof(Usuario));
            services.AddScoped(typeof(UtilPeticion));
            services.AddScoped(typeof(Conexion));
            services.AddScoped(typeof(UtilGeneral));
            services.AddScoped(typeof(LoggingService));
            services.AddSingleton(typeof(RedisCacheWrapper));
            services.AddScoped(typeof(Configuracion));
            services.AddScoped(typeof(GnossCache));
            services.AddScoped<IServicesUtilVirtuosoAndReplication, ServicesVirtuosoAndBidirectionalReplicationOpen>();
            services.AddScoped<ILabelerService, LabelerArtificialIntelligenceOpenService>();
            services.AddScoped(typeof(RelatedVirtuosoCL));
            services.AddScoped<IAvailableServices, AvailableServicesOpen>();
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
            if (bdType.Equals("2") || bdType.Equals("1"))
            {
                services.AddScoped(typeof(DbContextOptions<EntityContext>));
                services.AddScoped(typeof(DbContextOptions<EntityContextBASE>));
            }
            services.AddSingleton(typeof(ConfigService));

            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // Tiempo de expiración   
                                                                //options.Cookie.Name = "AppTest";
                                                                //options.Cookie.HttpOnly = true; // correct initialization

            });
            services.AddMvc();

            if (bdType.Equals("0"))
            {
                services.AddDbContext<EntityContext>();
                services.AddDbContext<EntityContextBASE>();
            }
            else if (bdType.Equals("1"))
            {
                services.AddDbContext<EntityContext, EntityContextOracle>();
                services.AddDbContext<EntityContextBASE, EntityContextBASEOracle>();
            }
            else if (bdType.Equals("2"))
            {
                services.AddDbContext<EntityContext, EntityContextPostgres>();
                services.AddDbContext<EntityContextBASE, EntityContextBASEPostgres>();
            }

            var sp = services.BuildServiceProvider();
            // Resolve the services from the service provider
            var loggingService = sp.GetService<LoggingService>();
            loggingService.AgregarEntrada("INICIO Application_Start");
            LoggingService.RUTA_DIRECTORIO_ERROR = Path.Combine(mEnvironment.ContentRootPath, "logs");
            loggingService.GuardarLog("Application_Start", logger);
            // Resolve the services from the service provider

            var entity = sp.GetService<EntityContext>();
			UtilServicios.CargarDominiosPermitidosCORS(entity);
            var hcConfigService = services.BuildServiceProvider().GetService<ConfigService>();
            services.AddHealthChecks()
                .AddGnossDatabaseHealthCheck<EntityContext>(bdType, hcConfigService.ObtenerSqlConnectionString())
                .AddGnossRedisHealthCheck(hcConfigService.ObtenerConexionRedisIPMaster("redis"))
                .AddGnossVirtuosoHealthCheck(hcConfigService.ObtenerVirtuosoConnectionString().ConnectionString)
                .AddGnossRabbitMQHealthCheck(hcConfigService.ObtenerRabbitMQClient(RabbitMQClient.BD_SERVICIOS_WIN));

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
            app.UseCors("_myAllowSpecificOrigins");
            app.UseSession();
            app.UseAuthorization();
            app.UseGnossMiddleware();
            var managementPort = Configuration.GetValue("ManagementPort", 8081);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGnossHealthEndpoints(managementPort);
                endpoints.MapControllers();
            });
        }
        
    }
}
