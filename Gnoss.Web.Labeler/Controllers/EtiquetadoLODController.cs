using Es.Riam.Gnoss.CL.LinkedOpenDataCL;
using Es.Riam.Gnoss.Elementos.LinkedOpenData;
using Es.Riam.Gnoss.Recursos;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Es.Riam.Util;
using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.InterfacesOpen;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Gnoss.Web.Labeler.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("_myAllowSpecificOrigins")]
    public class EtiquetadoLODController : Controller
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

        private bool mHayConexionLOD;

        public EtiquetadoLODController(EntityContext entityContext, LoggingService loggingService, ConfigService configService, RedisCacheWrapper redisCacheWrapper, VirtuosoAD virtuosoAD, IHttpContextAccessor httpContextAccessor, GnossCache gnossCache, EntityContextBASE entityContextBASE, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication, ILabelerService labelerService)
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
            mHayConexionLOD = mConfigService.ObtenerConexionRedisBD("lod") > 0;
            mUtilServicios = new UtilServicios(loggingService, entityContext, configService, redisCacheWrapper, gnossCache, servicesUtilVirtuosoAndReplication);
        }

        [HttpGet]
        [Route("ObtenerEntidadesLOD")]
        public IActionResult ObtenerEntidadesLOD(string documentoID, string tags, string urlBaseEnlaceTag, string idioma)
        {
            try
            {
                StringBuilder resultados = new StringBuilder();
                if (mHayConexionLOD)
                {
                    string languageCode = idioma.Replace("\"", "");

                    Guid docID = new Guid(documentoID.Replace("\"", ""));

                    string[] separadores = { "," };
                    string[] listaTags = tags.Replace("\"", "").Split(separadores, StringSplitOptions.RemoveEmptyEntries);

                    LinkedOpenDataCL LodCL = new LinkedOpenDataCL("lod", mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                    LodCL.Dominio = ObtenerDominioAplicacion();

                    Dictionary<string, EntidadLOD> resultadosCache = LodCL.ObtenerListaResourcesDeListaResultados(docID);

                    UtilIdiomas utilIdiomas = new UtilIdiomas(languageCode, mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper);

                    foreach (string tag in listaTags)
                    {
                        string etiqueta = tag.Trim();
                        if (resultadosCache.ContainsKey(etiqueta))
                        {
                            string descripcionLOD = ObtenerTagConEntidadesLod(etiqueta, resultadosCache[etiqueta], utilIdiomas);

                            if (!string.IsNullOrEmpty(resultados.ToString()))
                            {
                                resultados.Append(", ");
                            }

                            resultados.Append($"\"{etiqueta}\":{JsonSerializer.Serialize(descripcionLOD)}");
                        }
                    }
                }
                else
                {
                    mLoggingService.GuardarLogError("Para poder usar etiquetado con linked data deben estar configuradas las variables de entorno 'redis__lod__io__master', 'redis__lod__io__read', 'redis__lod__bd' y 'redis__lod___timeout'.");
                }
                string funcionCallBack = Request.Query["callback"];

                return Ok($"{funcionCallBack} ({{{resultados}}});");
            }
            catch (Exception ex)
            {
                mLoggingService.GuardarLogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Devuelve el dominio de la aplicación
        /// </summary>
        /// <returns></returns>
        private string ObtenerDominioAplicacion()
        {
            string dominio = mEntityContext.ParametroAplicacion.Where(item => item.Parametro.Equals("UrlIntragnoss")).Select(item => item.Valor).FirstOrDefault();

            if (string.IsNullOrEmpty(dominio))
            {
                dominio = "gnoss.com";
            }

            dominio = dominio.Replace("http://", string.Empty).Replace("www.", string.Empty);

            if (dominio[dominio.Length - 1] == '/')
            {
                dominio = dominio.Substring(0, dominio.Length - 1);
            }

            return dominio;
        }

        private string ObtenerTagConEntidadesLod(string pTag, EntidadLOD pEntidadDbPedia, UtilIdiomas pUtilIdiomas)
        {
            string descripcion = pEntidadDbPedia.Descripcion;
            string uriDbPedia = pEntidadDbPedia.Url;
            List<string> listaSameAs = pEntidadDbPedia.ListaEntidadesSameAs;

            StringBuilder descriptionFB = new StringBuilder();

            descriptionFB.Append($"<p class=\"titulo\"> {pUtilIdiomas.GetText("FREEBASE", "INFOSOBRE", pTag)}</p>");

            if (!string.IsNullOrEmpty(uriDbPedia))
            {
                descriptionFB.Append($"<a href='{uriDbPedia}' target='_blank'><div class='dbpedia'><p class='resource'><strong>DBpedia</strong></p></a>");
            }
            else
            {
                descriptionFB.Append("<div class='dbpedia'><p class='resource'><strong>DBpedia</strong></p>");
            }

            string descripcionEntidad = descripcion;

            //Codifico la caden en UTF8, si no da problemas con los acentos

            descripcionEntidad = UtilCadenas.TextoCortado(descripcionEntidad, 90);

            descriptionFB.Append($"<ul><li>{descripcionEntidad}</li>");
            descriptionFB.Append("<li class='moreLinks'>");
            descriptionFB.Append("<span class='moreInfo'>");
            descriptionFB.Append($"<a href='{ObtenerUrlWikipediaDesdeDbpedia(uriDbPedia)}' class='wikipedia' target='_blank'></a>");
            descriptionFB.Append("</span>");

            foreach (string sameAs in listaSameAs)
            {
                if (sameAs.Contains("sws.geonames.org"))
                {
                    descriptionFB.Append("<span class='moreInfo'>");
                    descriptionFB.Append($"<a href='{sameAs}' class='geonames' target='_blank'></a>");
                    descriptionFB.Append("</span>");
                }
                else if (sameAs.Contains("rdf.freebase.com"))
                {
                    descriptionFB.Append("<span class='moreInfo'>");
                    descriptionFB.Append($"<a href='{sameAs}' class='freeBase' target='_blank'></a>");
                    descriptionFB.Append("</span>");
                }
                else if (sameAs.Contains("topics.nytimes.com") || sameAs.Contains("data.nytimes.com"))
                {
                    descriptionFB.Append("<span class='moreInfo'>");
                    descriptionFB.Append($"<a href='{sameAs}' class='newYorkTimes' target='_blank'></a>");
                    descriptionFB.Append("</span>");
                }
            }
            descriptionFB.Append("</li>");
            descriptionFB.Append("</ul>");
            descriptionFB.Append("</div>");

            return descriptionFB.ToString();
        }

        /// <summary>
        /// Obtiene la url de wikipedia a partir de una url de dbpedia
        /// </summary>
        /// <param name="pUriDbpedia">Url de Dbpedia</param>
        /// <returns></returns>
        private string ObtenerUrlWikipediaDesdeDbpedia(string pUriDbpedia)
        {
            string urlWikipedia = "";

            if (pUriDbpedia.StartsWith("http://dbpedia.org/resource"))
            {
                urlWikipedia = pUriDbpedia.Replace("dbpedia.org/resource", "en.wikipedia.org/wiki");
            }
            else if (pUriDbpedia.StartsWith("http://es.dbpedia.org/resource"))
            {
                urlWikipedia = pUriDbpedia.Replace("es.dbpedia.org/resource", "es.wikipedia.org/wiki");
            }

            return urlWikipedia;
        }
    }
}
