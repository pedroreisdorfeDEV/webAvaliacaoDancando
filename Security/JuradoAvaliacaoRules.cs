namespace WebAvaliacaoDancando.Security;

public static class JuradoAvaliacaoRules
{
    public const short PrimeiroNumeroSuportado = 1;
    public const short UltimoNumeroSuportado = 4;

    public static bool IsNumeroSuportado(short juradoNumero)
    {
        return juradoNumero is >= PrimeiroNumeroSuportado and <= UltimoNumeroSuportado;
    }

    public static string BuildPerfilNaoHabilitadoMensagem(short juradoNumero)
    {
        return juradoNumero > 0
            ? $"O login informado esta vinculado ao jurado {juradoNumero}, mas este sistema esta configurado apenas para os jurados 1 a 4."
            : "Nao foi possivel identificar o numero do jurado logado.";
    }
}
