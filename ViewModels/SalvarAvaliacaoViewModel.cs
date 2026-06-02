using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAvaliacaoDancando.ModelBinders;

namespace WebAvaliacaoDancando.ViewModels;

public sealed class SalvarAvaliacaoViewModel
{
    [Required]
    public long ApresentacaoId { get; set; }

    [Required]
    public string SessaoKey { get; set; } = string.Empty;

    [ModelBinder(BinderType = typeof(FlexibleDecimalModelBinder))]
    [Range(typeof(decimal), "0", "10", ErrorMessage = "A nota deve estar entre 0 e 10.")]
    public decimal Nota { get; set; }

    public IFormFile? AudioArquivo { get; set; }
}
