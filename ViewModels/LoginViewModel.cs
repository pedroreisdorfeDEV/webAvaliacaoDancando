using System.ComponentModel.DataAnnotations;

namespace WebAvaliacaoDancando.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Informe o usuário.")]
    [Display(Name = "Usuário")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    public string? FeedbackMensagem { get; set; }
}
