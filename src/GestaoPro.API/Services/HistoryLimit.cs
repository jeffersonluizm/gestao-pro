namespace GestaoPro.API.Services;

// Históricos que crescem a cada atualização (ocorrência, vaga, visitante) guardam só as últimas entradas.
// No banco, o mesmo teto vai no $push com $slice (repositórios).
public static class HistoryLimit
{
    public const int Max = 50;

    public static void Trim<T>(List<T> history)
    {
        if (history.Count > Max) history.RemoveRange(0, history.Count - Max);
    }
}
