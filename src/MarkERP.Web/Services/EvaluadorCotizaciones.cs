using MarkERP.Web.Models;

namespace MarkERP.Web.Services;

/// <summary>
/// Calcula qué cotización es más conveniente.
/// Puntaje de 0 a 100 = 50 % precio + 30 % tiempo de entrega + 20 % calificación del proveedor.
/// La cotización más barata obtiene los 50 puntos de precio; las demás, proporcionalmente.
/// </summary>
public static class EvaluadorCotizaciones
{
    public const decimal PesoPrecio = 50m;
    public const decimal PesoEntrega = 30m;
    public const decimal PesoCalificacion = 20m;

    public record Opcion(int Id, decimal Total, int TiempoEntregaDias, decimal Calificacion);

    public static Dictionary<int, decimal> Calcular(IReadOnlyCollection<Opcion> opciones)
    {
        var resultado = new Dictionary<int, decimal>();
        if (opciones.Count == 0) return resultado;

        var minTotal = opciones.Where(o => o.Total > 0).Select(o => o.Total).DefaultIfEmpty(0).Min();
        var minDias = Math.Max(1, opciones.Min(o => Math.Max(1, o.TiempoEntregaDias)));

        foreach (var o in opciones)
        {
            var precio = o.Total <= 0 ? 0 : PesoPrecio * (minTotal / o.Total);
            var entrega = PesoEntrega * (minDias / (decimal)Math.Max(1, o.TiempoEntregaDias));
            var calificacion = PesoCalificacion * (Math.Clamp(o.Calificacion, 0, 5) / 5m);
            resultado[o.Id] = Math.Round(precio + entrega + calificacion, 1);
        }
        return resultado;
    }

    /// <summary>Asigna el puntaje a cada cotización de la lista (requiere el proveedor cargado).</summary>
    public static void Puntuar(IEnumerable<Cotizacion> cotizaciones)
    {
        var lista = cotizaciones.ToList();
        var puntajes = Calcular(lista.Select((c, i) => new Opcion(i, c.Total, c.TiempoEntregaDias, c.Proveedor?.Calificacion ?? 3)).ToList());
        for (var i = 0; i < lista.Count; i++) lista[i].Puntaje = puntajes[i];
    }

    public static Cotizacion? Recomendada(IEnumerable<Cotizacion> cotizaciones) =>
        cotizaciones.OrderByDescending(c => c.Puntaje).ThenBy(c => c.Total).FirstOrDefault();
}
