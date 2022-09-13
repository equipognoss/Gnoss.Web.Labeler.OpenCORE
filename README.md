![](https://content.gnoss.ws/imagenes/proyectos/personalizacion/7e72bf14-28b9-4beb-82f8-e32a3b49d9d3/cms/logognossazulprincipal.png)

# Gnoss.Web.Labeler.OpenCORE

![](https://github.com/equipognoss/Gnoss.Web.Labeler.OpenCORE/workflows/BuildLabeler/badge.svg)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=bugs)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Labeler.OpenCORE&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Labeler.OpenCORE)

Aplicación Web que ofrece etiquetas a partir de un título y/o una descripción. Se usa en la edición de recursos para proponerle etiquetas al usuario. 

Configuración estandar de esta aplicación en el archivo docker-compose.yml: 

```yml
labeler:
    image: gnoss/gnoss.web.labeler.opencore
    env_file: .env
    ports:
     - ${puerto_labeler}:80
    environment:
     virtuosoConnectionString: ${virtuosoConnectionString}
     virtuosoConnectionString_home: ${virtuosoConnectionString_home}
     acid: ${acid}
     base: ${base}
     redis__redis__ip__master: ${redis__redis__ip__master}
     redis__redis__ip__read: ${redis__redis__ip__read}
     redis__redis__bd: ${redis__redis__bd}
     redis__redis__timeout: ${redis__redis__timeout}
     redis__recursos__ip__master: ${redis__recursos__ip__master}
     redis__recursos__ip__read: ${redis__recursos__ip__read}
     redis__recursos__bd: ${redis__recursos__bd}
     redis__recursos__timeout: ${redis__redis__timeout}
     idiomas: ${idiomas}
     Servicios__urlBase: ${Servicios__urlBase}
     connectionType: ${connectionType}
    volumes:
      - ./logs/labeler:/app/logs
      - ./logs/labeler:/app/trazas
```

Se pueden consultar los posibles valores de configuración de cada parámetro aquí: https://github.com/equipognoss/Gnoss.Platform.Deploy


## Código de conducta
Este proyecto a adoptado el código de conducta definido por "Contributor Covenant" para definir el comportamiento esperado en las contribuciones a este proyecto. Para más información ver https://www.contributor-covenant.org/

## Licencia
Este producto es parte de la plataforma [Gnoss Semantic AI Platform Open Core](https://github.com/equipognoss/Gnoss.SemanticAIPlatform.OpenCORE), es un producto open source y está licenciado bajo GPLv3.
