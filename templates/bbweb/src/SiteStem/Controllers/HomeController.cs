using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bennewitz.Ninja.SiteStem.Controllers;

public sealed partial class HomeController(ILogger<HomeController> logger) : Controller
{
    public IActionResult Index() => View();

    /// <summary>
    /// Every error page: the status-code pages re-execute here for a 404 and its kin, and the
    /// exception handler for a 500. A code outside 400–599 in the URL renders as a 404. Each is logged
    /// once, with the address that was asked for.
    /// </summary>
    [HttpGet("/error/{code:int}")]
    public IActionResult Status(int code)
    {
        if (code is < 400 or > 599)
        {
            code = 404;
        }

        string path = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath
            ?? HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path
            ?? Request.Path.Value
            ?? string.Empty;

        LogStatusPage(logger, code, path);
        Response.StatusCode = code;
        return View("Status", new StatusPage(code, code == 404 ? "Not found" : "Something went wrong"));
    }

    // [Area.Kind], then key=value pairs. The path is free text, so the template quotes it, and
    // Serilog's {Message:lj} writes it as it is, quoted once ("Log lines", in the family's repository
    // conventions).
    [LoggerMessage(Level = LogLevel.Information, Message = "[Site.Status] code={Code} path=\"{Path}\"")]
    private static partial void LogStatusPage(ILogger logger, int code, string path);
}

/// <summary>What the error view shows: the code and a title, never an exception.</summary>
public sealed record StatusPage(int Code, string Title);
