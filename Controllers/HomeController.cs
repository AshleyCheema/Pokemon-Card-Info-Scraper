using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pokémon_Card_Info_Scraper_MVC.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using HtmlAgilityPack;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace Pokémon_Card_Info_Scraper_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private string url = "https://pkmncards.com/sets/";
        private string sortWebsite = "?auto&display=full";

        JsonSerializerOptions jsonS = new JsonSerializerOptions();


        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            jsonS.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            jsonS.WriteIndented = true;

            var response = CallURL(url).Result;
            GetPokemonSetLinks(response);
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static async Task<string> CallURL(string url)
        {
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            client.DefaultRequestHeaders.Accept.Clear();
            var response = client.GetStringAsync(url);
            return await response;
        }

        [Serializable]
        public class CardStats
        {
            public string image { get; set; }
            public string name { get; set; }
            public string hp { get; set; }
            public string type { get; set; }
            public string pokemons { get; set; }
            public string stage { get; set; }

            public string[] moves { get; set; }

            public string weakness { get; set; }
            public string resist { get; set; }
            public string retreat { get; set; }

            public string illusName { get; set; }

            public string series { get; set; }
            public string set { get; set; }
            public string cardNo { get; set; }
            public string rarity { get; set; }
            public string date { get; set; }

            public string flavorText { get; set; }
        }
        
        private void GetPokemonSetLinks(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var pokemonSets = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']");
            var pList = pokemonSets.Descendants("li").ToList();

            List<string> pokemonSetLinks = new List<string>();

            foreach (var link in pList)
            {
                if(link.InnerHtml.ToString().Contains("https:"))
                { 
                    pokemonSetLinks.Add(link.FirstChild.Attributes[0].Value + sortWebsite);
                }
            }

            GetPokemonCardLinks(pokemonSetLinks);
        }

        private async Task GetPokemonCardLinks(List<string> setLinks)
        {
            HtmlDocument doc = new HtmlDocument();

            foreach (var item in setLinks)
            {
                var response = await CallURL(item);
                doc.LoadHtml(response);
                var card = doc.DocumentNode.SelectNodes("//div[@class='card-image-area']").ToList();
                var name = doc.DocumentNode.SelectNodes("//span[@class='name']").ToList();
                var hp = doc.DocumentNode.SelectNodes("//span[@class='hp']").ToList();
                var type = doc.DocumentNode.SelectNodes("//span[@class='type']").ToList();
                var pokemon = doc.DocumentNode.SelectNodes("//span[@class='pokemons']").ToList();
                var stage = doc.DocumentNode.SelectNodes("//span[@class='stage']").ToList();

                var moves = doc.DocumentNode.SelectNodes("//div[@class='text']").ToList();
                var weakness = doc.DocumentNode.SelectNodes("//span[@class='weak']").ToList();
                var resist = doc.DocumentNode.SelectNodes("//span[@class='resist']").ToList();
                var retreat = doc.DocumentNode.SelectNodes("//span[@class='retreat']").ToList();

                var illusName = doc.DocumentNode.SelectNodes("//div[@class='illus minor-text']").ToList();

                var series = doc.DocumentNode.SelectNodes("//span[@title='Series']").ToList();
                var set = doc.DocumentNode.SelectNodes("//span[@title='Set']").ToList();
                var cardNo = doc.DocumentNode.SelectNodes("//span[@class='number-out-of']").ToList();
                var rarity = doc.DocumentNode.SelectNodes("//span[@class='rarity']").ToList();
                var date = doc.DocumentNode.SelectNodes("//span[@class='date']").ToList();

                var flavorText = doc.DocumentNode.SelectNodes("//div[@class='flavor minor-text']").ToList();

                GetPokemonData(card, name, hp, type, pokemon, stage, moves, weakness, resist, retreat, illusName, series, set, cardNo, rarity, date, flavorText);
            }
        }

        private void GetPokemonData(List<HtmlNode> card, List<HtmlNode> name, List<HtmlNode> hp, 
                                    List<HtmlNode> type, List<HtmlNode> pokemons, List<HtmlNode> stage,
                                    List<HtmlNode> moves, List<HtmlNode> weakness, List<HtmlNode> resist, List<HtmlNode> retreat, 
                                    List<HtmlNode> illusName, List<HtmlNode> series, List<HtmlNode> set, List<HtmlNode> cardNo,
                                    List<HtmlNode> rarity, List<HtmlNode> date, List<HtmlNode> flavorText)
        {
            for (int i = 0; i < card.Count; i++)
            {
                CardStats cardStats = new CardStats();

                cardStats.image = card[i].ChildNodes[0].Attributes[0].Value;
                cardStats.name = name[i].InnerText;
                cardStats.hp = hp[i].InnerText;
                cardStats.type = type[i].InnerText;
                cardStats.pokemons = pokemons[i].InnerText;
                cardStats.stage = stage[i].InnerText;

                cardStats.moves = CollatMoveData(moves[i]);

                cardStats.weakness = CollateStringData(weakness[i]);
                cardStats.resist = CollateStringData(resist[i]);
                cardStats.retreat = CollateStringData(retreat[i]);

                cardStats.illusName = illusName[i].InnerText;

                cardStats.series = CollateStringData(series[i]);
                cardStats.set = CollateStringData(set[i]);
                cardStats.cardNo = CollateStringData(cardNo[i]);
                cardStats.rarity = CollateStringData(rarity[i]);
                cardStats.date = CollateStringData(date[i]);

                cardStats.flavorText = HttpUtility.HtmlDecode(flavorText[i].InnerText);

                WriteToJSON(cardStats);
            }
        }

        private string[] CollatMoveData(HtmlNode data)
        {
            List<string> collatedData = new List<string>();

            for (int i = 0; i < data.ChildNodes.Count; i++)
            {
                string value = "";

                if (data.ChildNodes[i].Name != "#text")
                {
                    for (int j = 0; j < data.ChildNodes[i].ChildNodes.Count; j++)
                    {
                        value += data.ChildNodes[i].ChildNodes[j].InnerText;
                    }

                    string val = HttpUtility.HtmlDecode(value);

                    collatedData.Add(val);
                }
            }

            return collatedData.ToArray();
        }

        private string CollateStringData(HtmlNode data)
        {
            string endString = "";

            for (int i = 0; i < data.ChildNodes.Count; i++)
            {
                endString += data.ChildNodes[i].InnerText;
            }

            string decodeString = HttpUtility.HtmlDecode(endString);

            return decodeString;
        }

        private void WriteToJSON(CardStats cardStats)
        {
            string json = JsonSerializer.Serialize(cardStats, jsonS);
            System.IO.File.WriteAllText(@"H:\jsonFile.json", json);
        }
    }
}
