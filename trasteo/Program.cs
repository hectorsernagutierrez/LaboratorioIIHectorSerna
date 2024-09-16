using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using trasteo;


public class RegistroAnimal
{
    public string? Animal { get; set; }
    public string Campo { get; set; }
    public string? Informacion { get; set; }
 
    public string? Descripcion { get; set; }
    public string? Velocidad { get; set; }
    public string? Imagen { get; set; }
    public string? NombreCientifico { get; set; }
    public string? LinkWikidata { get; set; }
    public string Familia { get; set; }
    public string[] ObrasPrado { get; set; }
    public int? numObra { get; set; }

    public string? sigloPop { get; set; }
    public Dictionary<string, string?> CamposAdicionales { get; set; } = new Dictionary<string, string?>();


}
// Clase para organizar los datos



public class RegistroAnimalMap : ClassMap<RegistroAnimal>
{
    public RegistroAnimalMap()
    {
        Map(m => m.Animal).Name("Animal").Optional();
        Map(m => m.Campo).Name("Campo").Optional();
        Map(m => m.Informacion).Name("Información").Optional();
    }
}

class Program
{
    static void Main()
    {
        // Ruta del archivo CSV
        string pathAnimalData = @"Archivos\Lab_data_alumno.csv";
        string AnimalDataPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathAnimalData);
        string outputFilePath = @"Resultado\ResultadoAnimalDataProcesado.csv";

        if (!File.Exists(AnimalDataPath))
        {
            Console.WriteLine("El archivo no existe.");
            return;
        }

        // Configuración del lector CSV
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            Encoding = Encoding.UTF8
        };

        using (var reader = new StreamReader(AnimalDataPath, Encoding.UTF8))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<RegistroAnimalMap>();
            var recordsSinNormalizar = csv.GetRecords<RegistroAnimal>().ToList();

            var records = NormalizacionAnimales(recordsSinNormalizar);

            // Listas para detectar duplicados o inconsistencias
            var urlList = new HashSet<string>();
            var fotosList = new Dictionary<string, string>();  // Mapa de Animal -> Foto
            var velocidadList = new Dictionary<string, string>(); // Mapa de Animal -> Velocidad

            #region AnalisisInicial
            //// Recorremos los registros para realizar las validaciones 
            //// Esta parte se integrará ahora al apartado de generar información y depurarla
            //// Ya se ha realizado el analisis inicial
            //foreach (var record in records)
            //{
            //    // Validar URLs
            //    if (record.Campo == "link wikidata" || record.Campo == "imagen")
            //    {
            //        if (!IsValidUrl(record.Informacion))
            //        {
            //            Console.WriteLine($"URL no válida para {record.Animal}: {record.Informacion}");
            //            // Aquí podrías intentar corregir la URL o marcarla como incorrecta
            //        }

            //        if (record.Campo == "link wikidata" && !urlList.Add(record.Informacion))
            //        {
            //            Console.WriteLine($"Duplicado de URL para {record.Animal}: {record.Informacion}");
            //        }
            //    }

            //    // Comprobar si hay imágenes duplicadas
            //    if (record.Campo == "imagen")
            //    {
            //        if (fotosList.ContainsKey(record.Animal))
            //        {
            //            if (fotosList[record.Animal] != record.Informacion)
            //            {
            //                Console.WriteLine($"Inconsistencia de fotos para {record.Animal}: diferentes imágenes registradas.");
            //            }
            //        }
            //        else
            //        {
            //            fotosList[record.Animal] = record.Informacion;
            //        }
            //    }

            //    // Comprobar si hay diferentes velocidades para el mismo animal
            //    if (record.Campo == "velocidad")
            //    {
            //        if (velocidadList.ContainsKey(record.Animal))
            //        {
            //            if (velocidadList[record.Animal] != record.Informacion)
            //            {
            //                Console.WriteLine($"Inconsistencia de velocidades para {record.Animal}: diferentes velocidades registradas.");
            //            }
            //        }
            //        else
            //        {
            //            velocidadList[record.Animal] = record.Informacion;
            //        }
            //    }

            //    // Validar que las descripciones no contienen información incorrecta
            //    if (record.Campo == "descripción")
            //    {
            //        if (record.Informacion != null && !record.Informacion.Contains(record.Animal, StringComparison.OrdinalIgnoreCase))
            //        {
            //            Console.WriteLine($"La descripción de {record.Animal} parece no corresponder al animal descrito.");
            //        }
            //    }
            //}

            // Código para analisis inicial
            //var numRegistros = records.Count();

            //var animalesConMasInformacion = records.GroupBy(r => r.Animal)
            //                            .OrderByDescending(g => g.Count())
            //                            .Select(g => new { Animal = g.Key, Registros = g.ToList(), Conteo = g.Count() })
            //                            .ToList();

            //Console.WriteLine($"Número total de registros: {numRegistros}");
            //Console.WriteLine($"Animales con más información:");

            //foreach (var animal in animalesConMasInformacion)
            //{
            //    Console.WriteLine($"{animal.Animal}: {animal.Conteo} registros");
            //    Console.WriteLine("Detalles de los registros:");

            //    foreach (var registro in animal.Registros)
            //    {
            //        Console.WriteLine($"  Campo: {registro.Campo}, Información: {registro.Informacion}");
            //    }
            //    Console.WriteLine(); 
            //} 
            #endregion


            var columnasAdicionales = new HashSet<string>();
            var animalDescripcion = new Dictionary<string, string>();

            var animalesAgrupados = records.GroupBy(r => r.Animal)
                                                       .Select(g =>
                                                       {
                                                           var animalInfo = new RegistroAnimal
                                                           {
                                                               Animal = g.Key
                                                           };

                                                           foreach (var registro in g)
                                                           {
                                                               // Usar un switch para aplicar diferentes validaciones según el campo
                                                               switch (registro.Campo?.ToLower())
                                                               {
                                                                   case "link wikidata":
                                                                       
                                                                       if (string.IsNullOrEmpty(animalInfo.LinkWikidata))
                                                                       {
                                                                           // Verificar si la URL es válida, si no lo es, corregirla
                                                                           if (IsValidUrl(registro.Informacion, @"^(http|https)://www\.wikidata\.org\/wiki\/.+$"))
                                                                           {
                                                                               animalInfo.LinkWikidata = registro.Informacion;
                                                                           }
                                                                           else
                                                                           {
                                                                               animalInfo.LinkWikidata = GenerarWikiDataURL(registro.Informacion, registro.Animal);
                                                                               Console.WriteLine($"Corrigiendo URL de Wikidata para {registro.Animal}: {animalInfo.LinkWikidata}");
                                                                           }
                                                                       }
                                                                       
                                                                       break;

                                                                   case "imagen":
                                                                       if (string.IsNullOrEmpty(animalInfo.Imagen) &&  IsValidUrl(registro.Informacion, @"^(http|https):\/\/.+\.(png|jpg|jpeg|gif|bmp|webp)$"))
                                                                       {
                                                                           animalInfo.Imagen = registro.Informacion;
                                                                       }
                                                                       break;

                                                                   case "descripción":
                                                                       

                                                                       if (string.IsNullOrEmpty(animalInfo.Descripcion) && registro.Informacion != null)
                                                                       {// animalInfo.Descripcion = registro.Informacion;
                                                                           if (registro.Informacion.Contains(registro.Animal, StringComparison.OrdinalIgnoreCase))
                                                                           {

                                                                               // Guardar la descripción correcta para el animal
                                                                               if (!animalDescripcion.ContainsKey(registro.Animal))
                                                                               {
                                                                                   animalDescripcion[registro.Animal] = registro.Informacion;
                                                                               }
                                                                               
                                                                           }


                                                                           else
                                                                           {
                                                                               var possibleAnimals = records
                                                                                   .Where(r => r.Animal != null && registro.Informacion.Contains(r.Animal, StringComparison.OrdinalIgnoreCase)&& !animalDescripcion.ContainsKey(r.Animal))
                                                                                   .Select(r => r.Animal)
                                                                                   .ToList();
                                                                               
                                                                               Console.WriteLine($"Reasignando descripción de {registro.Animal} a {possibleAnimals.First()}");
                                                                               if (!animalDescripcion.ContainsKey(possibleAnimals.First()))
                                                                               {
                                                                                   animalDescripcion.Add(possibleAnimals.First(), registro.Informacion);
                                                                               }

                                                                           }
                                                               }
                                                                       break;

                                                                   case "velocidad":
                                                                       if (string.IsNullOrEmpty(animalInfo.Velocidad))
                                                                       {
                                                                           animalInfo.Velocidad = registro.Informacion;
                                                                       }
                                                                       break;
                                                                   case "nombre común":
                                                                       if (string.IsNullOrEmpty(animalInfo.NombreCientifico))
                                                                       {
                                                                           animalInfo.NombreCientifico = registro.Informacion;
                                                                       }
                                                                       break;



                                                                   default:
                                                                       // Campos adicionales que no están mapeados explícitamente
                                                                       if (!animalInfo.CamposAdicionales.ContainsKey(registro.Campo))
                                                                       {
                                                                           animalInfo.CamposAdicionales[registro.Campo] = registro.Informacion;
                                                                           columnasAdicionales.Add(registro.Campo);
                                                                       }
                                                                       break;
                                                               }
                                                           }

                                                           return animalInfo;
                                                       }).ToList(); 

            foreach( var animal in animalesAgrupados ) {
                if (animalDescripcion.ContainsKey(animal.Animal))
                {
                    //Console.WriteLine($"Reasignando descripción a {animal.Animal} {animalDescripcion[animal.Animal]}");
                    animal.Descripcion = animalDescripcion[animal.Animal];
                   
                }
            }
            
            using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            using (var csvOutput = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // Escribir encabezado con los campos explícitos
                csvOutput.WriteField("Animal");
                csvOutput.WriteField("Descripción");
                csvOutput.WriteField("Imagen");
                csvOutput.WriteField("LinkWikidata");
                csvOutput.WriteField("Velocidad");
                csvOutput.WriteField("Nombre Cientifico");
                csvOutput.WriteField("Familia");
                csvOutput.WriteField("FamiliaDescr");
                csvOutput.WriteField("Numero Obras");
                csvOutput.WriteField("Obras Prado");
                csvOutput.WriteField("Siglo Más popular");

                // Añadir dinámicamente las columnas adicionales
                foreach (var columna in columnasAdicionales)
                {
                    csvOutput.WriteField(columna);
                }
                csvOutput.NextRecord();

                // Escribir los datos
                foreach (var animal in animalesAgrupados)
                {
                    csvOutput.WriteField(animal.Animal);
                    csvOutput.WriteField(animal.Descripcion);
                    csvOutput.WriteField(animal.Imagen);
                    csvOutput.WriteField(animal.LinkWikidata);
                    csvOutput.WriteField(animal.Velocidad);
                    string wikicientifico = WikidataHelper.ObtenerNombreCientifico(animal.LinkWikidata);
                    //Notar que aqui con haber escrito el nombre cientifico solo desde wikidata servia pero de esta manera hcemos la comprobación pedida
                    if (wikicientifico.Equals(animal.NombreCientifico))
                    {
                        Console.WriteLine($"El nombre cientifico no coincide con el de wikidata {wikicientifico} vs {animal.NombreCientifico}");
                        
                        csvOutput.WriteField(wikicientifico);
                    }
                    else
                    {

                        csvOutput.WriteField(!string.IsNullOrEmpty(animal.NombreCientifico) ? animal.NombreCientifico :wikicientifico);
                    }
                    var (familia, familiaDescr) = WikidataHelper.ObtenerFamiliaYPropiedades(animal.LinkWikidata);
                    csvOutput.WriteField(familia);
                    csvOutput.WriteField(familiaDescr);
                    var (numeroObras, obrasNombres, sigloMasPopular) = PradoHelper.ObtenerInfoPrado(animal.Animal.ToLower());
                    csvOutput.WriteField(numeroObras);
                    csvOutput.WriteField(obrasNombres);
                    csvOutput.WriteField(sigloMasPopular);
                    foreach (var columna in columnasAdicionales)
                    {
                        if (animal.CamposAdicionales.TryGetValue(columna, out var valor))
                        {
                            csvOutput.WriteField(valor);
                        }
                        else
                        {
                            csvOutput.WriteField(""); 
                        }
                    }
                    csvOutput.NextRecord();
                }
            }

            Console.WriteLine($"Archivo CSV creado exitosamente en: {outputFilePath}");
        }
    }

    /// <summary>
    /// Devuelve true si el URL es correcto
    /// </summary>
    /// <param name="url">string</param>
    /// <returns>bool</returns>
    public static bool IsValidUrl(string url,string patron)
    {
        if (string.IsNullOrEmpty(url))
            return false;//Uso Regex para generar un patron de url
       
        return Regex.IsMatch(url, patron);
    }
    /// <summary>
    /// Genera el contenido del campo wikidata
    /// </summary>
    /// <param name="url"></param>
    /// <param name="animal"></param>
    /// <returns></returns>
    public static string GenerarWikiDataURL(string url, string animal)
    {
        // Buscar si el código de la entidad empieza por 'Q' y extraerlo
        var match = Regex.Match(url, @"Q\d+");
        if (match.Success)
        {
            return $"https://www.wikidata.org/wiki/{match.Value}";
        }
        else
        {
            Console.WriteLine($"No se pudo corregir la URL de Wikidata para {animal}");
            return $"https://www.wikidata.org/wiki/Q";
        }
    }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        public static List<RegistroAnimal> NormalizacionAnimales(List<RegistroAnimal> records)
    {
        var animalSynonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "burro", "asno" },  
            { "asno", "asno" },
            {"camaleón","camaleon" }
            
        };
        var registrosNormalizados = new List<RegistroAnimal>();

        
        foreach (var record in records)
        {
            if (record.Animal != null && animalSynonyms.ContainsKey(record.Animal))
            {                
                record.Animal = animalSynonyms[record.Animal];
            }
                        
            registrosNormalizados.Add(record);
        }
        return registrosNormalizados;


    }
}