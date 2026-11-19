using MarkERP.Web.Models;
using MarkERP.Web.Services;
using Xunit;

namespace MarkERP.Tests;

public class CalculosTests
{
    [Fact]
    public void Edad_se_calcula_con_cumpleanos_no_cumplido()
    {
        var nacimiento = new DateTime(2000, 12, 25);
        Assert.Equal(25, Calculos.AniosCumplidos(nacimiento, new DateTime(2026, 11, 18)));
        Assert.Equal(26, Calculos.AniosCumplidos(nacimiento, new DateTime(2026, 12, 25)));
    }

    [Fact]
    public void Antiguedad_en_texto()
    {
        Assert.Equal("2 años y 3 meses", Calculos.TextoAntiguedad(new DateTime(2024, 8, 1), new DateTime(2026, 11, 18)));
        Assert.Equal("1 mes", Calculos.TextoAntiguedad(new DateTime(2026, 10, 1), new DateTime(2026, 11, 18)));
        Assert.Equal("1 año", Calculos.TextoAntiguedad(new DateTime(2025, 11, 18), new DateTime(2026, 11, 18)));
    }

    [Fact]
    public void Totales_de_venta_aplican_descuento_antes_del_iva()
    {
        var (subtotal, descuento, iva, total) = Calculos.TotalesVenta(new[] { (2, 15000m), (1, 32000m) }, 10, 19);
        Assert.Equal(62000m, subtotal);
        Assert.Equal(6200m, descuento);
        Assert.Equal(10602m, iva);
        Assert.Equal(66402m, total);
    }

    [Fact]
    public void Empleado_calcula_imc()
    {
        var e = new Empleado { EstaturaCm = 176, PesoKg = 74 };
        Assert.Equal(23.9m, e.Imc);
    }
}

public class EvaluadorCotizacionesTests
{
    [Fact]
    public void La_mas_barata_y_rapida_obtiene_100()
    {
        var p = EvaluadorCotizaciones.Calcular(new[]
        {
            new EvaluadorCotizaciones.Opcion(1, 1000, 2, 5),
            new EvaluadorCotizaciones.Opcion(2, 2000, 4, 5)
        });
        Assert.Equal(100m, p[1]);
        Assert.Equal(25m + 15m + 20m, p[2]);
    }

    [Fact]
    public void Recomienda_por_puntaje_no_solo_por_precio()
    {
        var cotizaciones = new List<Cotizacion>
        {
            new() { Id = 1, Total = 950, TiempoEntregaDias = 10, Proveedor = new Proveedor { Calificacion = 3 } },
            new() { Id = 2, Total = 1000, TiempoEntregaDias = 1, Proveedor = new Proveedor { Calificacion = 5 } }
        };
        EvaluadorCotizaciones.Puntuar(cotizaciones);
        Assert.Equal(2, EvaluadorCotizaciones.Recomendada(cotizaciones)!.Id);
    }

    [Fact]
    public void Puntua_cotizaciones_nuevas_sin_id()
    {
        var cotizaciones = new List<Cotizacion>
        {
            new() { Total = 500, TiempoEntregaDias = 3, Proveedor = new Proveedor { Calificacion = 4 } },
            new() { Total = 600, TiempoEntregaDias = 3, Proveedor = new Proveedor { Calificacion = 4 } }
        };
        EvaluadorCotizaciones.Puntuar(cotizaciones);
        Assert.True(cotizaciones[0].Puntaje > cotizaciones[1].Puntaje);
    }
}

public class ConexionTests
{
    [Fact]
    public void Convierte_url_de_render_a_cadena_npgsql()
    {
        var cadena = ConexionPostgres.DesdeUrl("postgresql://markerp:cl%40ve@dpg-abc123-a/markerp");
        Assert.Equal("Host=dpg-abc123-a;Port=5432;Database=markerp;Username=markerp;Password=cl@ve;SSL Mode=Prefer", cadena);
    }
}
