using Microsoft.AspNetCore.Mvc;

namespace ProyectoDeProbabilidadYEstadistica.Controllers;

public class ProblemsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
