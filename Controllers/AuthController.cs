using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAvaliacaoDancando.Extensions;
using WebAvaliacaoDancando.Security;
using WebAvaliacaoDancando.Services;
using WebAvaliacaoDancando.ViewModels;

namespace WebAvaliacaoDancando.Controllers;

public class AuthController(IAuthService authService) : Controller
{
    [AllowAnonymous]
    [HttpGet("/Login")]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var juradoNumero = User.GetJuradoNumero();
            if (JuradoAvaliacaoRules.IsNumeroSuportado(juradoNumero))
            {
                return RedirectToAction("Index", "Avaliacao");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View(new LoginViewModel
            {
                FeedbackMensagem = JuradoAvaliacaoRules.BuildPerfilNaoHabilitadoMensagem(juradoNumero)
            });
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
            ModelState.AddModelError(string.Empty, "Usuario ou senha invalidos.");
            return View(model);
        }

        var juradoNumero = principal.GetJuradoNumero();
        if (!JuradoAvaliacaoRules.IsNumeroSuportado(juradoNumero))
        {
            ModelState.AddModelError(
                string.Empty,
                JuradoAvaliacaoRules.BuildPerfilNaoHabilitadoMensagem(juradoNumero));
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
        TempData["FeedbackMensagem"] = "Sessao encerrada com sucesso.";
        return RedirectToAction(nameof(Login));
    }
}
