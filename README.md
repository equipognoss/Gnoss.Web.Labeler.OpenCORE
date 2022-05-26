# Gnoss.Web.Labeler.OpenCORE

Aplicación Web que ofrece etiquetas a partir de un título y/o una descripción. Se usa en la edición de recursos para proponerle etiquetas al usuario. 

Configuración estandar de esta aplicación en el archivo docker-compose.yml: 

```yml
autocompletaretiquetas:
    image: etiquetadoautomatico
    env_file: .env
    ports:
     - ${puerto_autocompletaretiquetas}:80
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
      - ./logs/etiquetadoautomatico:/app/logs
      - ./logs/etiquetadoautomatico:/app/trazas
```

Se pueden consultar los posibles valores de configuración de cada parámetro aquí: https://github.com/equipognoss/Gnoss.Platform.Deploy
