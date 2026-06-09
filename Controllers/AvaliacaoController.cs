using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAvaliacaoDancando.Extensions;
using WebAvaliacaoDancando.Security;
using WebAvaliacaoDancando.Services;
using WebAvaliacaoDancando.ViewModels;

namespace WebAvaliacaoDancando.Controllers;

[Authorize]
public class AvaliacaoController(
    IAvaliacaoService avaliacaoService,
    ILogger<AvaliacaoController> logger) : Controller
{
    [HttpGet("/Avaliacao")]
    public async Task<IActionResult> Index(string? sessao, CancellationToken cancellationToken)
    {
        if (!JuradoValido())
        {
            return await RedirectJuradoInvalidoAsync();
        }

        return View(await BuildViewModelAsync(sessao, cancellationToken));
    }

    [HttpPost("/Avaliacao/Salvar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(SalvarAvaliacaoViewModel model, CancellationToken cancellationToken)
    {
        if (!JuradoValido())
        {
            return await RedirectJuradoInvalidoAsync();
        }

        if (!ModelState.IsValid)
        {
            logger.LogWarning(
                "ModelState invalido ao salvar avaliacao {ApresentacaoId}. Erros: {Erros}",
                model.ApresentacaoId,
                string.Join(
                    " | ",
                    ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .SelectMany(entry => entry.Value!.Errors.Select(error =>
                            $"{entry.Key}: {error.ErrorMessage}"))));

            var viewModel = await BuildViewModelAsync(model.SessaoKey, cancellationToken);
            return View("Index", viewModel);
        }

        //if (model.AudioArquivo is null || model.AudioArquivo.Length == 0)
        //{
        //    var jaTemParecer = await avaliacaoService.ApresentacaoJaTemParecerAsync(
        //        model.ApresentacaoId,
        //        User.GetJuradoNumero(),
        //        cancellationToken);
        //
        //    if (!jaTemParecer)
        //    {
        //        ModelState.AddModelError(string.Empty, "Grave um audio antes de salvar a avaliacao.");
        //        var viewModel = await BuildViewModelAsync(model.SessaoKey, cancellationToken);
        //        return View("Index", viewModel);
        //    }
        //}

        try
        {
            await avaliacaoService.SaveAsync(model, User.GetJuradoNumero(), cancellationToken);
            TempData["FeedbackMensagem"] = "Avaliacao salva com sucesso.";
            TempData["FeedbackTipo"] = "success";
            return RedirectToAction(nameof(Index), new { sessao = model.SessaoKey });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar a avaliacao da apresentacao {ApresentacaoId}", model.ApresentacaoId);
            ModelState.AddModelError(string.Empty, ex.Message);
            var viewModel = await BuildViewModelAsync(model.SessaoKey, cancellationToken);
            return View("Index", viewModel);
        }
    }

    private async Task<AvaliacaoViewModel> BuildViewModelAsync(string? sessaoKey, CancellationToken cancellationToken)
    {
        var viewModel = await avaliacaoService.GetViewModelAsync(
            sessaoKey,
            User.GetJuradoNumero(),
            User.Identity?.Name ?? "Jurado",
            cancellationToken);

        viewModel.FeedbackMensagem = TempData["FeedbackMensagem"] as string;
        viewModel.FeedbackTipo = TempData["FeedbackTipo"] as string;

        return viewModel;
    }

    private bool JuradoValido()
    {
        return JuradoAvaliacaoRules.IsNumeroSuportado(User.GetJuradoNumero());
    }

    private async Task<IActionResult> RedirectJuradoInvalidoAsync()
    {
        var juradoNumero = User.GetJuradoNumero();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["FeedbackMensagem"] = JuradoAvaliacaoRules.BuildPerfilNaoHabilitadoMensagem(juradoNumero);
        return RedirectToAction("Login", "Auth");
    }
}
