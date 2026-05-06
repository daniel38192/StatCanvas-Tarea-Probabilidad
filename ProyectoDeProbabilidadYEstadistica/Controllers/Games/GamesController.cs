using Microsoft.AspNetCore.Mvc;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.Games;

public class GamesController: Controller
{
   public IActionResult Game1() => View();
   public IActionResult Game2() => View(); 
}