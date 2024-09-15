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
            var records = csv.GetRecords<RegistroAnimal>().ToList();

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
                                                                       if (string.IsNullOrEmpty(animalInfo.LinkWikidata) && IsValidUrl(registro.Informacion, @"^(http|https)://www\.wikidata\.org\/wiki\/.+$"))
                                                                       {
                                                                           animalInfo.LinkWikidata = registro.Informacion;
                                                                       }
                                                                       break;

                                                                   case "imagen":
                                                                       if (string.IsNullOrEmpty(animalInfo.Imagen) &&  IsValidUrl(registro.Informacion, @"^(http|https):\/\/.+\.(png|jpg|jpeg|gif|bmp|webp)$"))
                                                                       {
                                                                           animalInfo.Imagen = registro.Informacion;
                                                                       }
                                                                       break;

                                                                   case "descripción":
                                                                       //if (animalDescripcion.ContainsKey(registro.Animal))
                                                                       //{
                                                                       //    animalInfo.Descripcion = animalDescripcion[registro.Animal];
                                                                       //}
                                                                       if (string.IsNullOrEmpty(animalInfo.Descripcion) && registro.Informacion != null)
                                                                       {// animalInfo.Descripcion = registro.Informacion;

                                                                           // Guardar la descripción correcta para el animal
                                                                           if (animalDescripcion.ContainsKey(registro.Animal))
                                                                           {
                                                                               animalDescripcion[registro.Animal] = registro.Informacion;
                                                                           }

                                                                       
                                                                       else
                                                                       {
                                                                           // Buscar el animal correcto al que debería pertenecer esta descripción
                                                                           var possibleAnimals = records
                                                                               .Where(r => r.Animal != null && registro.Informacion.Contains(r.Animal, StringComparison.OrdinalIgnoreCase))
                                                                               .Select(r => r.Animal)
                                                                               .ToList();
                                                                               // Si encontramos solo un animal posible, reasignamos la descripción a ese animal
                                                                               Console.WriteLine($"Reasignando descripción de {registro.Animal} a {possibleAnimals.First()}");
                                                                               animalDescripcion.Add(possibleAnimals.First(), registro.Informacion) ;
                                                                               
                                                                       }
                                                               }
                                                                       break;

                                                                   case "velocidad":
                                                                       if (string.IsNullOrEmpty(animalInfo.Velocidad))
                                                                       {
                                                                           animalInfo.Velocidad = registro.Informacion;
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

                    // Escribir los campos adicionales
                    foreach (var columna in columnasAdicionales)
                    {
                        if (animal.CamposAdicionales.TryGetValue(columna, out var valor))
                        {
                            csvOutput.WriteField(valor);
                        }
                        else
                        {
                            csvOutput.WriteField(""); // Si no hay valor para la columna, dejar en blanco
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
    public static bool IsValidUrl(string url,string pattern)
    {
        if (string.IsNullOrEmpty(url))
            return false;//Uso Regex para generar un patron de url
       
        return Regex.IsMatch(url, pattern);
    }
}