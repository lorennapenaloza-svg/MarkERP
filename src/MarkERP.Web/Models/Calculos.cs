using System.Globalization;

namespace MarkERP.Web.Models;

/// <summary>Cálculos puros reutilizables (fáciles de probar con xUnit).</summary>
public static class Calculos
{
    public static readonly CultureInfo Co = CultureInfo.GetCultureInfo("es-CO");

    public static int AniosCumplidos(DateTime desde, DateTime hasta)
    {
        var anios = hasta.Year - desde.Year;
        if (hasta.Date < desde.Date.AddYears(anios)) anios--;
        return Math.Max(anios, 0);
    }

    public static string TextoAntiguedad(DateTime desde, DateTime hasta)
    {
        if (hasta < desde) return "0 meses";
        var meses = (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month;
        if (hasta.Day < desde.Day) meses--;
        meses = Math.Max(meses, 0);
        var a = meses / 12;
        var m = meses % 12;
        if (a == 0) return $"{m} {(m == 1 ? "mes" : "meses")}";
        if (m == 0) return $"{a} {(a == 1 ? "año" : "años")}";
        return $"{a} {(a == 1 ? "año" : "años")} y {m} {(m == 1 ? "mes" : "meses")}";
    }

    public static decimal Redondear(decimal valor) => Math.Round(valor, 0, MidpointRounding.AwayFromZero);

    public static (decimal subtotal, decimal descuento, decimal iva, decimal total) TotalesVenta(
        IEnumerable<(int cantidad, decimal precio)> lineas, decimal porcentajeDescuento, decimal porcentajeIva)
    {
        var subtotal = lineas.Sum(l => l.cantidad * l.precio);
        var descuento = Redondear(subtotal * porcentajeDescuento / 100m);
        var baseGravable = subtotal - descuento;
        var iva = Redondear(baseGravable * porcentajeIva / 100m);
        return (subtotal, descuento, iva, baseGravable + iva);
    }

    public static string Moneda(decimal valor) => "$" + valor.ToString("N0", Co);
}
