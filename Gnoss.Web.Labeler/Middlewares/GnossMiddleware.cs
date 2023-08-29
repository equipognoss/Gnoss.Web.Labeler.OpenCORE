using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.Util.General;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServicioAutoCompletarMVC
{
    public class GnossMiddleware
    {
        private IHostingEnvironment mEnv;
        private readonly RequestDelegate _next;

        public GnossMiddleware(RequestDelegate next, IHostingEnvironment env)
        {
            _next = next;
            mEnv = env;
        }

        public async Task Invoke(HttpContext context, EntityContext entityContext)
        {
            entityContext.SetTrackingFalse();
            await _next(context);
        }
    }

    public static class GlobalAsaxExtensions
    {
        public static IApplicationBuilder UseGnossMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GnossMiddleware>();
        }
    }

}

