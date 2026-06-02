using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAvaliacaoDancando.Services;
using WebAvaliacaoDancando.ViewModels;

namespace WebAvaliacaoDancando.Controllers;

public class AuthController(IAuthService authService) : Controller
{
    [AllowAnonymous]
    [HttpGet("/Login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Avaliacao");
        }

        return View(new LoginViewModel
        {
            FeedbackMensagem = TempData["FeedbackMensagem"] as string
        });
    }

    [AllowAnonymous]
    [HttpPost("/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var principal = await authService.AuthenticateAsync(model.Login, model.Senha, cancellationToken);
        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            return View(model);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return RedirectToAction("Index", "Avaliacao");
    }

    [Authorize]
    [HttpPost("/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["FeedbackMensagem"] = "Sessão encerrada com sucesso.";
        return RedirectToAction(nameof(Login));
    }
}
