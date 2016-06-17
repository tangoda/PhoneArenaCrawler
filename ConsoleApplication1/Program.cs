using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;

namespace ConsoleApplication1
{
    class Program
    {
        private static Dictionary<string, List<string>> Modelos;

        static void Main(string[] args)
        {
            var urlBase = "http://www.phonearena.com";
            Modelos = new Dictionary<string, List<string>>();

            var webClient = new WebClient();
            var html = webClient.DownloadString(urlBase + "/phones/manufacturers");
            Console.WriteLine(urlBase + "/phones/manufacturers");

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var linksFabricantes = new Dictionary<string, string>();

            var titulos = doc.DocumentNode.SelectNodes("//*[contains(@class,'title')]");

            foreach (var titulo in titulos)
            {
                var link = titulo.ParentNode;
                var fabricante = titulo.InnerText;

                if (!Modelos.Keys.Contains(fabricante))
                    Modelos.Add(fabricante, new List<string>());

                linksFabricantes.Add(fabricante, urlBase + link.Attributes["href"].Value);
            }

            ProcessaPaginaFabricante(linksFabricantes);
            GerarArquivoCSV();
        }

        private static void ProcessaPaginaFabricante(Dictionary<string, string> linksFabricantes)
        {
            foreach (var link in linksFabricantes)
            {
                var webClient = new WebClient();
                var html = webClient.DownloadString(link.Value);

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var ultimaPagina = ObterUltimaPagina(doc);

                var modelos = new List<string>();

                for (int i = 1; i <= ultimaPagina; i++)
                {
                    html = webClient.DownloadString(link.Value + "/page/" + i.ToString());
                    Console.WriteLine(link.Value + "/page/" + i.ToString());
                    doc.LoadHtml(html);

                    var phones = doc.DocumentNode.SelectNodes("//div[@data-sub_obj_type]//*[contains(@class,'title')]");

                    if (phones == null)
                        continue;

                    foreach (var phone in phones)
                    {
                        var modelo = phone.InnerText;
                        if (!modelos.Contains(modelo))
                            modelos.Add(modelo);
                    }
                }

                Modelos[link.Key] = modelos;
            }
        }

        private static void GerarArquivoCSV()
        {
            var sw = new StreamWriter(File.Open(@"D:\modelos.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite));

            foreach (var marca in Modelos)
            {
                foreach (var modelo in (List<string>)marca.Value)
                {
                    sw.WriteLine(string.Format("{0};{1}", marca.Key, modelo));
                }
            }

            sw.Close();
            sw.Dispose();
        }

        private static int ObterUltimaPagina(HtmlDocument doc)
        {
            var controlePaginacao = doc.DocumentNode.SelectNodes("//*[contains(@class,'s_pager')]//ul");
            if (controlePaginacao == null)
                return 1;

            var patternUltimaPagina = @"changePage\((?<UltimaPagina>[0-9]*)\,.*";
            var ultimaPaginaMatch = Regex.Match(controlePaginacao.Nodes().Last().InnerHtml, patternUltimaPagina);
            var ultimaPagina = 0;
            if (ultimaPaginaMatch.Success)
                return Convert.ToInt32(ultimaPaginaMatch.Groups["UltimaPagina"].Value);
            return 1;
        }
    }
}
