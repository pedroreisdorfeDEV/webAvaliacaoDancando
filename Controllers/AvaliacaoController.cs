using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAvaliacaoDancando.Extensions;
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
            return RedirectToAction("Login", "Auth");
        }

        return View(await BuildViewModelAsync(sessao, cancellationToken));
    }

    [HttpPost("/Avaliacao/Salvar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(SalvarAvaliacaoViewModel model, CancellationToken cancellationToken)
    {
        if (!JuradoValido())
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            logger.LogWarning(
                "ModelState inválido ao salvar avaliação {ApresentacaoId}. Erros: {Erros}",
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

        //    if (!jaTemParecer)
        //    {
        //        ModelState.AddModelError(string.Empty, "Grave um áudio antes de salvar a avaliação.");
        //        var viewModel = await BuildViewModelAsync(model.SessaoKey, cancellationToken);
        //        return View("Index", viewModel);
        //    }
        //}

        try
        {
            await avaliacaoService.SaveAsync(model, User.GetJuradoNumero(), cancellationToken);
            TempData["FeedbackMensagem"] = "Avaliação salva com sucesso.";
            TempData["FeedbackTipo"] = "success";
            return RedirectToAction(nameof(Index), new { sessao = model.SessaoKey });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar a avaliação da apresentação {ApresentacaoId}", model.ApresentacaoId);
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
        var juradoNumero = User.GetJuradoNumero();
        return juradoNumero is >= 1 and <= 3;
    }
}
