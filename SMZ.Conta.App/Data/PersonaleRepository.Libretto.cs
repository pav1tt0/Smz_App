using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    public List<RegistroImmersioneRiga> GetLibrettoSommozzatore(int perId)
    {
        using var connection = OpenConnection();
        return GetRegistroImmersioniMensile(connection, DateOnly.MinValue, DateOnly.MaxValue, perId)
            .OrderBy(item => item.DataServizio).ThenBy(item => item.ServizioGiornalieroId)
            .ThenBy(item => item.NumeroImmersione).ThenBy(item => item.Apparato).ToList();
    }
}
