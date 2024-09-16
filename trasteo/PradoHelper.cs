
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

public class PradoHelper
{
    /// <summary>
    /// Método auxiliar para obtener el número total de obras, sus nombres y el siglo más popular para un animal.
    /// </summary>
    /// <param name="animal">El nombre del animal.</param>
    /// <returns>Una tupla con el número de obras, el nombre de todas las obras y el siglo más popular.</returns>
    public static (int numeroObras, string obrasNombres, string sigloMasPopular) ObtenerInfoPrado(string animal)
    {
        var obrasNombres = new List<string>();
        var siglosContador = new Dictionary<int, int>(); // Contador de obras por siglo
        int pagina = 1;
        var rdfResult = ObtenerRDFDelPrado(animal, pagina).TrimStart();

        // Crear un XmlDocument
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(rdfResult);

        var siglos = new List<int>();

        // Obtener el número de obras
        var numeroObrasNodo = int.Parse(xmlDoc.SelectSingleNode("//gnoss:NumberOfResults", ObtenerNamespaceManager(xmlDoc))?.InnerText);

        int numPags = (numeroObrasNodo / 18)+1 ;

        for (pagina = 1; pagina < numPags; pagina++)
        {
            // Hacer la petición paginada al RDF del Prado
            rdfResult = ObtenerRDFDelPrado(animal, pagina);

            // Parsear el resultado RDF
            var obrasPagina = ParsearRDFObras(rdfResult, out var obrasSiglos);
            siglos.AddRange(obrasSiglos);

            obrasNombres.AddRange(obrasPagina);
        }

        // Contar los siglos
        foreach (var siglo in siglos)
        {
            if (siglosContador.ContainsKey(siglo))
            {
                siglosContador[siglo]++;
            }
            else
            {
                siglosContador[siglo] = 1;
            }
        }

        // Obtener el siglo con más obras
        var sigloMasPopular = siglosContador.Count > 0
            ? siglosContador.Aggregate((l, r) => l.Value > r.Value ? l : r).Key.ToString()
            : "N/A";

        return (numeroObrasNodo, string.Join("|", obrasNombres), sigloMasPopular);
    }

    /// <summary>
    /// Método para obtener el RDF del Prado para un animal específico.
    /// </summary>
    /// <param name="animal">El nombre del animal.</param>
    /// <param name="pagina">La página actual para paginar resultados.</param>
    /// <returns>El contenido RDF de la respuesta.</returns>
    public static string ObtenerRDFDelPrado(string animal, int pagina)
    {
        var options = new RestClientOptions("https://www.museodelprado.es")
        {
            MaxTimeout = -1,
        };
        var client = new RestClient(options);
        var request = new RestRequest($"/coleccion/obras-de-arte?searchObras={animal.ToLower()}&rdf=null&pagina={pagina}", Method.Get);
        request.AddHeader("Accept", "application/rdf+xml");

        var response = client.Execute(request);

        if (response.IsSuccessful)
        {
            return response.Content;
        }
        else
        {
            Console.WriteLine($"Error al obtener RDF del Prado para {animal}: {response.ErrorMessage}");
            return "";
        }
    }

    /// <summary>
    /// Parsear el RDF para extraer los nombres de las obras y sus siglos.
    /// </summary>
    /// <param name="rdfContent">El contenido RDF.</param>
    /// <param name="obrasSiglos">Lista de los siglos de las obras.</param>
    /// <returns>Una lista con los nombres de las obras.</returns>
    public static List<string> ParsearRDFObras(string rdfContent, out List<int> obrasSiglos)
    {
        var obras = new List<string>();
        obrasSiglos = new List<int>();

        if (string.IsNullOrEmpty(rdfContent)) return obras;

        try
        {
            // Crear un XmlDocument y cargar el contenido
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(rdfContent);

            // Seleccionar los nodos 'Results'
            var obrasNodos = xmlDoc.SelectNodes("//gnoss:Results", ObtenerNamespaceManager(xmlDoc));

            foreach (XmlNode obra in obrasNodos)
            {
                var url = obra.Attributes["rdf:resource"]?.InnerText;

                if (!string.IsNullOrEmpty(url))
                {
                    var (titulo, siglo) = BuscarTituloYSigloDeObra(url);
                   // string patron = @"obra-de-arte/([a-zA-Z0-9\-]+)/";

                    //Match match = Regex.Match(url, patron);
                    obras.Add(titulo); // Agregar la URL del recurso como nombre de obra match.Groups[1].Value
                    obrasSiglos.Add(siglo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al parsear RDF: {ex.Message}");
        }

        return obras;
    }

    /// <summary>
    /// Método auxiliar para buscar el siglo de una obra basada en su URL.
    /// </summary>
    /// <param name="obraUrl">URL de la obra.</param>
    /// <returns>El siglo de la obra.</returns>
    public static (string titulo, int siglo) BuscarTituloYSigloDeObra(string obraUrl)
    {
        var options = new RestClientOptions("https://www.museodelprado.es")
        {
            MaxTimeout = -1,
        };
        var client = new RestClient(options);
        var request = new RestRequest($"{obraUrl}?rdf=null", Method.Get);
        request.AddHeader("Accept", "application/rdf+xml");

        var response = client.Execute(request);

        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Error al obtener RDF para la obra: {response.ErrorMessage}");
            return (null, -1);
        }

        try
        {
            // Crear un XmlDocument y cargar el contenido RDF
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response.Content);

            // Obtener el título de la obra usando el nodo 'ecidoc:p102_E35_p3_has_title_augmented'
            var titulo = xmlDoc.SelectSingleNode("//ecidoc:p102_E35_p3_has_title_augmented", ObtenerNamespaceManager(xmlDoc))?.InnerText;
            if (string.IsNullOrEmpty(titulo))
            {
                //Console.WriteLine("No se encontró un título para la obra.");
                return (null, 0);
            }

            // Obtener el primer 'textDate' y tratar de identificar si contiene un año o un siglo
            var textDateNode = xmlDoc.SelectSingleNode("//ecidoc:p62_E52_p79_has_time-span_beginning", ObtenerNamespaceManager(xmlDoc))?.InnerText;
            int siglo = -1;

            if (!string.IsNullOrEmpty(textDateNode))
            {
                // Intentar extraer un año de 4 dígitos o un siglo
                if (int.TryParse(textDateNode.Substring(0, 4), out int anio))
                {
                    siglo = (anio / 100) + 1; // Convertir año a siglo
                }
                else if (textDateNode.Contains("siglo", StringComparison.OrdinalIgnoreCase))
                {
                    // Extraer el número del siglo mencionado
                    var sigloMatch = System.Text.RegularExpressions.Regex.Match(textDateNode, @"\d+");
                    if (sigloMatch.Success)
                    {
                        siglo = int.Parse(sigloMatch.Value);
                    }
                }
            }

            // Si no se encuentra fecha válida, devuelve -1
            if (siglo == -1)
            {
                Console.WriteLine("No se pudo determinar el siglo.");
            }

            // Mostrar la información obtenida
           // Console.WriteLine($"Título de la obra: {titulo}");
          //  Console.WriteLine($"Siglo de la obra: {siglo}");

            return (titulo, siglo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al parsear RDF: {ex.Message}");
            return (null, -1);
        }
    }

    /// <summary>
    /// Método para crear un XmlNamespaceManager basado en los espacios de nombres RDF utilizados.
    /// </summary>
    /// <param name="xmlDoc">El documento XML sobre el cual trabajar.</param>
    /// <returns>El XmlNamespaceManager con los espacios de nombres.</returns>
    private static XmlNamespaceManager ObtenerNamespaceManager(XmlDocument xmlDoc)
    {
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("gnoss", "http://gnoss.com/gnoss.owl#");
        nsmgr.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        nsmgr.AddNamespace("ecidoc", "http://museodelprado.es/ontologia/ecidoc.owl#");
        return nsmgr;
    }
}
