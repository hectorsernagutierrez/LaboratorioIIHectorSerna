# Descripción del Programa

Este programa está diseñado para procesar un archivo CSV con información sobre animales, realizando un tratamiento exhaustivo de los datos para corregir errores y completar la información utilizando fuentes externas. Su objetivo principal es garantizar la integridad y coherencia de la información, mientras agrega datos adicionales provenientes de Wikidata y el Museo del Prado.Viendo así la relación de los animales en el arte y su representación en este.

## Características

### 1. **Corrección de Errores en el CSV**
   - **Duplicados**: El programa identifica y elimina registros duplicados en el archivo CSV, asegurando que no existan entradas repetidas.
   - **Mala asociación de animales**: Revisa y corrige las asociaciones incorrectas entre obras de arte y los animales mencionados en sus descripciones.
   - **Descripciones incorrectas**: Detecta y corrige errores en las descripciones de las obras, mejorando la calidad de los datos.

### 2. **Validación y Corrección de URIs**
   - El programa revisa las URIs asociadas a las obras de arte para verificar que sean válidas. Si se detecta un problema en la URI, se corrige automáticamente o se notifica al usuario.

### 3. **Enriquecimiento de Datos con Wikidata y el Museo del Prado**
   - **Wikidata**: El programa utiliza la API de Wikidata para obtener información adicional sobre las familias y nombres científicos de los animales.
   - **Museo del Prado**: El Museo del Prado se consulta para obtener detalles específicos sobre las obras en la colección, complementando la información existente en el CSV con datos de índole artística.

### 4. **Salida de Datos Mejorada**
   Tras el procesamiento, el programa genera un nuevo archivo CSV con la información corregida y enriquecida, listo para ser utilizado en otros sistemas o reportes.

## Instrucciones de Uso

1. **Preparación del archivo CSV**: 
   - Asegúrate de tener el archivo CSV en el formato esperado.
   -En la carpeta de documentación tiene un html explicando el código. \repos\trasteo\trasteo\Documentación\html\index.html

2. **Ejecución del Programa**:
   - Carga el archivo CSV en el programa. 
   - El programa procesará el archivo, identificando y corrigiendo los errores mencionados.
   - Los datos serán enriquecidos con información adicional proveniente de Wikidata y el Museo del Prado.
   -En caso de que no funcione pruebe a copiar la carpeta de Archivos Y Resultados dentro de repos\trasteo\trasteo\bin\Debug\net6.0

3. **Salida de Datos**:
   - Una vez completado el proceso, el programa generará un nuevo archivo CSV con toda la información corregida y complementada dentro de la carpeta Resultados de C:\Users\usuario\source\repos\trasteo\trasteo\bin\Debug\net6.0

### Requisitos

- **Conexión a Internet**: Es necesaria para consultar las APIs de Wikidata y el Museo del Prado.
- **Formato CSV**: El archivo CSV debe estar correctamente formateado . Vale el modificaciones del csv siempre que sean ás registros. pero nunca más columnas.

## API Utilizadas

- **Wikidata API**: Se usa para obtener información adicional sobre las familias
- **Museo del Prado**: Se consulta para obtener detalles sobre las obras de arte en la colección del Museo del Prado.

## Errores y Advertencias

- El programa está diseñado para manejar automáticamente los errores comunes. Sin embargo, si se encuentran problemas críticos que no pueden corregirse, se notificará al usuario con detalles específicos sobre los registros problemáticos.
