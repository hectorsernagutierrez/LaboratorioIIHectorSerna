using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


using RestSharp;
using System;
using System.Text.RegularExpressions;
namespace trasteo;

public class WikidataHelper
{
    private static readonly RestClient client = new RestClient(new RestClientOptions("https://query.wikidata.org") { MaxTimeout = -1 });

    /// <summary>
    /// Obtiene la familia y las propiedades de un animal 
    /// desde Wikidata basado en su identificador quye empieaz cn Q.
    /// </summary>
    /// <param name="wikidataUrl">La URL de la entidad en Wikidata.</param>
    /// <returns>Una tupla con el nombre de la familia y las propiedades concatenadas.</returns>
    public static (string familia, string familiaDescr) ObtenerFamiliaYPropiedades(string wikidataUrl)
    {
        // Extraer el identificador Q de la URL de Wikidata (ej. Q726)
        var match = Regex.Match(wikidataUrl, @"Q\d+");
        if (!match.Success)
        {
            //Console.WriteLine($"Identificador con Q no encontrado en la URL: {wikidataUrl}");
            return ("", "");
        }

        string wikidataId = match.Value;
        Console.WriteLine($"Consultando familia y propiedades para {wikidataId}...");

        // Consulta SPARQL a Wikidata para obtener la familia y las propiedades
        var query = $@"
            SELECT DISTINCT ?familiaLabel ?propertyLabel ?valueLabel
            WHERE {{
              wd:{wikidataId} wdt:P279 ?familia.
              ?familia wdt:P31 wd:Q16521.
              ?familia wdt:P105 wd:Q35409.
              ?familia ?property ?value.
              FILTER(LANG(?value) = 'es')
              SERVICE wikibase:label {{ bd:serviceParam wikibase:language 'es,mul' }}
            }}";

        var request = new RestRequest($"/sparql?query={Uri.EscapeDataString(query)}", Method.Get);
        request.AddHeader("Accept", "text/csv");

        // Hacer la solicitud y obtener la respuesta
        var response = client.Execute(request);
        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Error al consultar Wikidata: {response.ErrorMessage}");
            return ("", "");
        }

        // Procesar la respuesta CSV
        var lines = response.Content.Split('\n');
        if (lines.Length <= 1)
        {
            Console.WriteLine("No se encontraron resultados para la consulta.");
            return ("", "");
        }

        string familia = "";
        string familiaDescr = "";

        // Procesar las líneas del CSV
        foreach (var line in lines.Skip(1))  // Saltar la cabecera
        {
            var columns = line.Split(',');
            if (columns.Length >= 3)
            {
                familia = columns[0].Trim(); // Columna familiaLabel
                string propiedad = columns[1].Trim(); // Columna propertyLabel
                string valor = columns[2].Trim(); // Columna valueLabel

                // Concatenar las propiedades en FamiliaDescr
                familiaDescr += $"{propiedad}: {valor}|";
            }
        }

        return (familia, familiaDescr); 
    }













     /// Obtiene el nombre científico de un animal desde Wikidata basado en su identificador Q.
    /// </summary>
    /// <param name="wikidataUrl">La URL de la entidad en Wikidata.</param>
    /// <returns>El nombre científico del animal, o una cadena vacía si no se encuentra.</returns>
    public static string ObtenerNombreCientifico(string wikidataUrl)
    {
        // Extraer el identificador Q de la URL de Wikidata (ej. Q726)
        var match = Regex.Match(wikidataUrl, @"Q\d+");
        if (!match.Success)
        {
            Console.WriteLine($"Identificador Q no encontrado en la URL: {wikidataUrl}");
            return "";
        }

        string wikidataId = match.Value;
        Console.WriteLine($"Consultando nombre científico para {wikidataId}...");

        // Consulta SPARQL a Wikidata para obtener el nombre científico (propiedad P225)
        var query = $@"
            SELECT ?nombreCientifico
            WHERE {{
              wd:{wikidataId} wdt:P225 ?nombreCientifico.
            }}";

        var request = new RestRequest($"/sparql?query={Uri.EscapeDataString(query)}", Method.Get);
        request.AddHeader("Accept", "text/csv");

        // Hacer la solicitud y obtener la respuesta
        var response = client.Execute(request);
        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Error al consultar Wikidata: {response.ErrorMessage}");
            return "";
        }

        // Procesar la respuesta CSV
        var lines = response.Content.Split('\n');
        if (lines.Length <= 1)
        {
            Console.WriteLine("No se encontró un nombre científico para la consulta.");
            return "";
        }

        // Extraer el nombre científico de la primera línea de resultados (después de la cabecera)
        var columns = lines[1].Split(',');
        if (columns.Length >= 1)
        {
            return columns[0].Trim(); // Columna nombreCientificoLabel
        }

        return "";
    }
}











