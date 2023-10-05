using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Trazas;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.InterfacesOpen;
using Es.Riam.Util;
using Es.Riam.Util.AnalisisSintactico;
using Gnoss.Web.LabelerService;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Gnoss.Web.Labeler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("_myAllowSpecificOrigins")]
    public class EtiquetadoAutomaticoController : Controller
    {
        private EntityContext mEntityContext;
        private LoggingService mLoggingService;
        private ConfigService mConfigService;
        private RedisCacheWrapper mRedisCacheWrapper;
        private VirtuosoAD mVirtuosoAD;
        private IHttpContextAccessor mHttpContextAccessor;
        private UtilServicios mUtilServicios;
        private GnossCache mGnossCache;
        private EntityContextBASE mEntityContextBASE;
        private IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;
        private ILabelerService mLabelerService;
        private static object BLOQUEO_COMPROBACION_TRAZA = new object();
        private static DateTime HORA_COMPROBACION_TRAZA;

        public EtiquetadoAutomaticoController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, IHttpContextAccessor httpContextAccessor, GnossCache gnossCache, EntityContextBASE entityContextBASE, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, ILabelerService labelerService)
        {
            mEntityContext = entityContext;
            mLoggingService = loggingService;
            mConfigService = configService;
            mVirtuosoAD = virtuosoAD;
            mRedisCacheWrapper = redisCacheWrapper;
            mHttpContextAccessor = httpContextAccessor;
            mGnossCache = gnossCache;
            mEntityContextBASE = entityContextBASE;
            mLabelerService = labelerService;  
            mUtilServicios = new UtilServicios(loggingService, entityContext, configService, redisCacheWrapper, gnossCache, servicesUtilVirtuosoAndReplication);
        }

        #region Metodos web

        [HttpPost]
        [Route("SeleccionarEtiquetasDesdeServicio")]
        public string SeleccionarEtiquetasDesdeServicio([FromForm] string titulo, [FromForm] string descripcion, [FromForm] string ProyectoID)
        {
            if (string.IsNullOrEmpty(ProyectoID))
            {
                if (Request.Form.ContainsKey("ProyectoID"))
                {
                    ProyectoID = Request.Form["ProyectoID"];
                }
                if (Request.Form.ContainsKey("descripcion"))
                {
                    descripcion = Request.Form["descripcion"];
                }
                if (Request.Form.ContainsKey("titulo"))
                {
                    titulo = Request.Form["titulo"];
                }

            }

            if (titulo == null)
            {
                titulo = "";
            }

            if (descripcion == null)
            {
                descripcion = "";
            }

            string resultadosDirectos = " ";
            string resultadosPropuestos = " ";

            try
            {
                titulo = HttpUtility.UrlDecode(titulo);
                descripcion = HttpUtility.UrlDecode(descripcion);
                UtilLabelerService utilLabelerService = new UtilLabelerService();
                resultadosDirectos = utilLabelerService.ObtenerEtiquetasDeTituloYDescripcion(titulo, descripcion, out resultadosPropuestos);
            }
            catch (Exception ex)
            {
                mUtilServicios.GuardarLog(ex.Message + "\r\nPila: " + ex.StackTrace, "error");
                throw;
            }
            return resultadosDirectos;
        }

        [HttpPost]
        [Route("SeleccionarEtiquetas")]
        public IActionResult SeleccionarEtiquetas([FromForm] string titulo, [FromForm] string descripcion, [FromForm] string ProyectoID, [FromForm] string documentoID, [FromForm] string extension)
        {
            try
            {
                if (string.IsNullOrEmpty(ProyectoID))
                {
                    if (Request.Form.ContainsKey("ProyectoID"))
                    {
                        ProyectoID = Request.Form["ProyectoID"];
                    }
                    if (Request.Form.ContainsKey("descripcion"))
                    {
                        descripcion = Request.Form["descripcion"];
                    }
                    if (Request.Form.ContainsKey("titulo"))
                    {
                        titulo = Request.Form["titulo"];
                    }
                }

                if (titulo == null)
                {
                    titulo = "";
                }

                if (descripcion == null)
                {
                    descripcion = "";
                }

                string resultados = " ";
                string resultadosPropuestos = " ";

                titulo = QuitarComillasTextoInicioYFinal(titulo);
                descripcion = QuitarComillasTextoInicioYFinal(descripcion);
                ProyectoID = QuitarComillasTextoInicioYFinal(ProyectoID);

                titulo = HttpUtility.UrlDecode(titulo);
                descripcion = UtilCadenas.ReemplazarCaracteresHTML(HttpUtility.UrlDecode(descripcion));
                ////////////////////////////////////////////////
                resultados = mLabelerService.ObtenerEtiquetas(titulo, descripcion, out resultadosPropuestos, ProyectoID, documentoID, extension);//ObtenerEtiquetasDeTituloYDescripcion(titulo, descripcion, out resultadosPropuestos);
                /////////////////////////////////////////
                //Obtengo los enlaces
                string resultadosEnlaces = "";

                string callback = Request.Query["callback"];
                if (string.IsNullOrEmpty(callback))
                {
                    callback = Request.Form["callback"];
                }
                string resultado = "{\"directas\":" + JsonSerializer.Serialize(resultados.ToLower()) + ", \"propuestas\":" + JsonSerializer.Serialize(resultadosPropuestos) + ", \"enlaces\":" + JsonSerializer.Serialize(resultadosEnlaces) + "}";
                resultado = callback + resultado;
                return Ok(resultado);
                //return Content(resultado, "application/json");
                /*this.Context.Response.ContentType = "text/plain";
                this.Context.Response.Write(funcionCallBack + "({\"directas\":" + JsonConvert.SerializeObject(resultados.ToLower()) + ", \"propuestas\":" + JsonConvert.SerializeObject(resultadosPropuestos) + ", \"enlaces\":" + JsonConvert.SerializeObject(resultadosEnlaces) + "});");
            */
            }
            catch (Exception ex)
            {
                mUtilServicios.GuardarLog(ex.Message + "\r\nPila: " + ex.StackTrace, "error");
                throw;
            }
        }

        [HttpPost]
        [Route("SeleccionarEtiquetasMultiple")]
        public IActionResult SeleccionarEtiquetasMultiple([FromForm] string titulo, [FromForm] string descripcion, [FromForm] string ProyectoID, [FromForm] string identificadorPeticion, [FromForm] string fin)
        {
            try
            {
                if (string.IsNullOrEmpty(ProyectoID))
                {
                    if (Request.Form.ContainsKey("ProyectoID"))
                    {
                        ProyectoID = Request.Form["ProyectoID"];
                    }
                    if (Request.Form.ContainsKey("descripcion"))
                    {
                        descripcion = Request.Form["descripcion"];
                    }
                    if (Request.Form.ContainsKey("titulo"))
                    {
                        titulo = Request.Form["titulo"];
                    }
                    if (Request.Form.ContainsKey("identificadorPeticion"))
                    {
                        identificadorPeticion = Request.Form["identificadorPeticion"];
                    }
                    if (Request.Form.ContainsKey("fin"))
                    {
                        fin = Request.Form["fin"];
                    }

                }



                Guid idPeticion = new Guid(identificadorPeticion.Replace("\"", ""));
                //titulo = HttpUtility.UrlDecode(titulo);
                //descripcion = HttpUtility.UrlDecode(descripcion);
                if (mHttpContextAccessor.HttpContext.Session.GetString(idPeticion.ToString()) != null)
                {
                    descripcion = descripcion.Substring(1, descripcion.Length - 2);
                    mHttpContextAccessor.HttpContext.Session.SetString(idPeticion.ToString(), mHttpContextAccessor.HttpContext.Session.GetString(idPeticion.ToString()) + descripcion );
                }
                else
                {
                    descripcion = descripcion.Substring(1, descripcion.Length - 2);
                    mHttpContextAccessor.HttpContext.Session.SetString(idPeticion.ToString(), descripcion);
                }


                if (fin == "\"true\"")
                {
                    string textoCompleto = mHttpContextAccessor.HttpContext.Session.GetString(idPeticion.ToString()).ToString();
                    textoCompleto = "\"" + textoCompleto + "\"";
                    mHttpContextAccessor.HttpContext.Session.Remove(idPeticion.ToString());
                    return SeleccionarEtiquetas(titulo, textoCompleto, ProyectoID, "", "");
                }
                else
                {
                    string callback = Request.Query["callback"];
                    if (string.IsNullOrEmpty(callback))
                    {
                        callback = Request.Form["callback"];
                    }
                    string resultado = "({\"siguiente\":true});";
                    resultado = callback + resultado;
                    return Ok(resultado);
                }
            }
            catch (Exception ex)
            {
                mUtilServicios.GuardarLog(ex.Message + "\r\nPila: " + ex.StackTrace, "error");
                throw;
            }
        }

        #endregion

        #region Métodos de trazas
        [NonAction]
        private void IniciarTraza()
        {
            if (DateTime.Now > HORA_COMPROBACION_TRAZA)
            {
                lock (BLOQUEO_COMPROBACION_TRAZA)
                {
                    if (DateTime.Now > HORA_COMPROBACION_TRAZA)
                    {
                        HORA_COMPROBACION_TRAZA = DateTime.Now.AddSeconds(15);
                        TrazasCL trazasCL = new TrazasCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                        string tiempoTrazaResultados = trazasCL.ObtenerTrazaEnCache("labeler");

                        if (!string.IsNullOrEmpty(tiempoTrazaResultados))
                        {
                            int valor = 0;
                            int.TryParse(tiempoTrazaResultados, out valor);
                            LoggingService.TrazaHabilitada = true;
                            LoggingService.TiempoMinPeticion = valor; //Para sacar los segundos
                        }
                        else
                        {
                            LoggingService.TrazaHabilitada = false;
                            LoggingService.TiempoMinPeticion = 0;
                        }
                    }
                }
            }
        }
        #endregion

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            IniciarTraza();
        }

        #region Métodos generales

        [NonAction]
        private string QuitarComillasTextoInicioYFinal(string pTexto)
        {
            if (pTexto.StartsWith("\"") && pTexto.EndsWith("\""))
            {
                pTexto = pTexto.Substring(1, pTexto.Length - 2);
            }

            return pTexto;
        }

       
        

        #endregion


    }
}
