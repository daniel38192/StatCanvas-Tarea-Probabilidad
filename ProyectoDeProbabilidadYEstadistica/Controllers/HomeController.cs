using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;
using ProyectoDeProbabilidadYEstadistica.Models.Home;

namespace ProyectoDeProbabilidadYEstadistica.Controllers;

public class HomeController : Controller
{
    private const string CortisolRankingAssetsPath = "/assets/png-cortisol-ranking";

    private static readonly IReadOnlyList<CortisolRankingEntryViewModel> CortisolRankingEntries =
    [
        new() { Name = "Daniel Gutiérrez", PhotoPath = $"{CortisolRankingAssetsPath}/daniel-600x600.jpeg", CortisolLevel = "low-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-cortisol.png" },
        new() { Name = "Enoc Cruz", PhotoPath = $"{CortisolRankingAssetsPath}/enoc-cruz-600x600.jpeg", CortisolLevel = "high-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/high-cortisol.png" },
        new() { Name = "Melquiades Sosa", PhotoPath = $"{CortisolRankingAssetsPath}/meliquedes-sosa-600x600.jpeg", CortisolLevel = "normal-high-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/normal-high-cortisol.png" },
        new() { Name = "Filiberto Grajeda", PhotoPath = $"{CortisolRankingAssetsPath}/fili-600x600.jpeg", CortisolLevel = "normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/normal-cortisol.png" },
        new() { Name = "Jesús Fernández", PhotoPath = $"{CortisolRankingAssetsPath}/jesus-fernandez.jpg", CortisolLevel = "normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/normal-cortisol.png" },
        new() { Name = "Jairo Sánchez", PhotoPath = $"{CortisolRankingAssetsPath}/jairo-Sanchez-600x600.jpeg", CortisolLevel = "low-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-cortisol.png" },
        new() { Name = "Natyelli Vélez", PhotoPath = $"{CortisolRankingAssetsPath}/Natyelli-600x600.jpeg", CortisolLevel = "low-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-cortisol.png" },
        new() { Name = "Mary Tolentino", PhotoPath = $"{CortisolRankingAssetsPath}/mary-Tolentino-600x600.jpeg", CortisolLevel = "high-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/high-cortisol.png" },
        new() { Name = "Sheny Pet", PhotoPath = $"{CortisolRankingAssetsPath}/sheny-test.png", CortisolLevel = "low-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-cortisol.png" },
        new() { Name = "Alejandro Garrido", PhotoPath = $"{CortisolRankingAssetsPath}/garrido.png", CortisolLevel = "low-normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-normal-cortisol.png" },
        new() { Name = "Daniel Neri", PhotoPath = $"{CortisolRankingAssetsPath}/Daniel-Neri-600x600.jpeg", CortisolLevel = "low-normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-normal-cortisol.png" },
        new() { Name = "Eliseo Guadarrama", PhotoPath = $"{CortisolRankingAssetsPath}/Eliseo-Guadarrama-646x649.jpg", CortisolLevel = "low-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-cortisol.png" },
        new() { Name = "Ignacio Cruz", PhotoPath = $"{CortisolRankingAssetsPath}/Ignacio-Cruz-600x600.jpeg", CortisolLevel = "low-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-cortisol.png" },
        new() { Name = "José Mendoza", PhotoPath = $"{CortisolRankingAssetsPath}/Jose-Maciste.jpg", CortisolLevel = "normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/normal-cortisol.png" },
        new() { Name = "Pablo Lemus", PhotoPath = $"{CortisolRankingAssetsPath}/Pablo-lemus.jpeg", CortisolLevel = "high-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/high-cortisol.png" },
        new() { Name = "Rubén López", PhotoPath = $"{CortisolRankingAssetsPath}/ruben.jpeg", CortisolLevel = "low-normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-normal-cortisol.png" },
        new() { Name = "Sem Barba", PhotoPath = $"{CortisolRankingAssetsPath}/sem-barba.jpeg", CortisolLevel = "low-normal-cortisol", MetricImagePath = $"{CortisolRankingAssetsPath}/low-normal-cortisol.png" }
    ];

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("cortisol-ranking")]
    public IActionResult CortisolRanking()
    {
        return View(CortisolRankingEntries);
    }

    [HttpGet("games")]
    public IActionResult Games()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
