using Microsoft.AspNetCore.Mvc;

namespace Bennewitz.Ninja.SiteStem.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>
    /// Every error page: the status-code pages re-execute here for a 404 and its kin, and the
    /// exception handler for a 500. A code outside 400–599 in the URL renders as a 404.
    /// </summary>
    [HttpGet("/error/{code:int}")]
    public IActionResult Status(int code)
    {
        if (code is < 400 or > 599)
        {
            code = 404;
        }

        Response.StatusCode = code;
        return View("Status", new StatusPage(code, code == 404 ? "Not found" : "Something went wrong"));
    }
}

/// <summary>What the error view shows: the code and a title, never an exception.</summary>
public sealed record StatusPage(int Code, string Title);
