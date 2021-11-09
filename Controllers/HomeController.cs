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
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.IO;
using System.Web;
using System.Text.Json;

namespace Pokémon_Card_Info_Scraper_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private string url = "https://pkmncards.com/sets/";
        private string sortWebsite = "?auto&display=full";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
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
                GetPokemonData(card, name, hp, type, pokemon, stage, moves);
            }
        }

        private void GetPokemonData(List<HtmlNode> card, List<HtmlNode> name, List<HtmlNode> hp, 
                                    List<HtmlNode> type, List<HtmlNode> pokemons, List<HtmlNode> stage,
                                    List<HtmlNode> moves)
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
                cardStats.moves = SetMoves(moves[i]);

                WriteToJSON(cardStats);
            }
        }

        private string[] SetMoves(HtmlNode moves)
        {
            List<string> moveList = new List<string>();

            for (int i = 0; i < moves.ChildNodes.Count; i++)
            {
                string value = "";

                if (moves.ChildNodes[i].Name != "#text")
                {
                    for (int j = 0; j < moves.ChildNodes[i].ChildNodes.Count; j++)
                    {
                        value += moves.ChildNodes[i].ChildNodes[j].InnerText;
                    }

                    string val = HttpUtility.HtmlDecode(value);

                    moveList.Add(val);
                }
            }

            return moveList.ToArray();
        }

        private void WriteToJSON(CardStats cardStats)
        {
            string json = JsonSerializer.Serialize(cardStats);

            System.IO.File.WriteAllText(@"H:\jsonFile.json", json);
        }
    }
}
