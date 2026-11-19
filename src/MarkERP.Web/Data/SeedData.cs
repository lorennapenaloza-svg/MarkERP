using MarkERP.Web.Models;
using MarkERP.Web.Services;
using Microsoft.AspNetCore.Identity;

namespace MarkERP.Web.Data;

/// <summary>Datos ficticios para que la demo se vea completa desde el primer arranque.</summary>
public static class SeedData
{
    public static void Cargar(AppDbContext db)
    {
        if (db.Usuarios.Any()) return;
        var rnd = new Random(42);
        var hoy = DateTime.Today;

        // ── Configuración y usuarios ──
        db.Configuracion.Add(new ConfiguracionNegocio
        {
            NombreNegocio = "Tienda Demo MARKERP",
            Nit = "901.234.567-8",
            Direccion = "Cra. 10 # 12-34, Chía, Cundinamarca",
            Telefono = "601 555 0101",
            Email = "contacto@markerp.demo",
            PorcentajeIva = 19,
            TemaPorDefecto = "algodon"
        });

        var hasher = new PasswordHasher<Usuario>();
        Usuario U(string nombre, string email, string rol, string clave)
        {
            var u = new Usuario { Nombre = nombre, Email = email, Rol = rol };
            u.PasswordHash = hasher.HashPassword(u, clave);
            return u;
        }
        db.Usuarios.AddRange(
            U("Administrador MARKERP", "admin@markerp.com", Roles.Admin, "Admin123*"),
            U("Valentina Ríos", "ventas@markerp.com", Roles.Vendedor, "Ventas123*"),
            U("Andrés Molina", "inventario@markerp.com", Roles.Inventario, "Inventario123*"),
            U("Camila Torres", "rrhh@markerp.com", Roles.TalentoHumano, "Rrhh123*"));

        // ── Catálogos ──
        var cats = new[]
        {
            new Categoria { Nombre = "Accesorios", Descripcion = "Aretes, collares, pulseras y anillos" },
            new Categoria { Nombre = "Bolsos y billeteras", Descripcion = "Bolsos, morrales y billeteras" },
            new Categoria { Nombre = "Cuidado personal", Descripcion = "Cosmética y cuidado" },
            new Categoria { Nombre = "Papelería", Descripcion = "Cuadernos, agendas y esferos" },
            new Categoria { Nombre = "Hogar", Descripcion = "Decoración y artículos para el hogar" },
            new Categoria { Nombre = "Tecnología", Descripcion = "Accesorios para celular y computador" }
        };
        db.Categorias.AddRange(cats);
        var bodegas = new[]
        {
            new Bodega { Nombre = "Bodega principal", Ubicacion = "Local 1 - trastienda" },
            new Bodega { Nombre = "Punto de venta", Ubicacion = "Vitrina y mostrador" }
        };
        db.Bodegas.AddRange(bodegas);

        var datosProductos = new (string cod, string nombre, int cat, decimal compra, decimal venta, int stock, int min)[]
        {
            ("ACC-001", "Aretes de plata 925", 0, 18000, 39000, 3, 6),
            ("ACC-002", "Collar dorado con dije", 0, 22000, 48000, 6, 8),
            ("ACC-003", "Pulsera tejida artesanal", 0, 6000, 15000, 40, 10),
            ("ACC-004", "Anillo ajustable acero", 0, 9000, 22000, 25, 8),
            ("ACC-005", "Set de pinzas para cabello", 0, 4000, 12000, 55, 15),
            ("BOL-001", "Bolso tote de lona", 1, 28000, 65000, 12, 5),
            ("BOL-002", "Billetera de cuero sintético", 1, 15000, 35000, 4, 6),
            ("BOL-003", "Morral urbano impermeable", 1, 45000, 99000, 9, 4),
            ("CUI-001", "Crema hidratante 250 ml", 2, 12000, 26000, 30, 10),
            ("CUI-002", "Brillo labial", 2, 5000, 14000, 48, 12),
            ("CUI-003", "Kit de brochas de maquillaje", 2, 20000, 45000, 7, 5),
            ("PAP-001", "Agenda 2027 tapa dura", 3, 14000, 32000, 22, 8),
            ("PAP-002", "Cuaderno argollado 100 hojas", 3, 6500, 14500, 60, 20),
            ("PAP-003", "Set de esferos pastel x6", 3, 7000, 16000, 2, 10),
            ("HOG-001", "Vela aromática de soya", 4, 11000, 27000, 18, 6),
            ("HOG-002", "Taza de cerámica pintada", 4, 9500, 24000, 14, 6),
            ("HOG-003", "Cojín decorativo", 4, 16000, 38000, 10, 4),
            ("TEC-001", "Cargador USB-C 20W", 5, 17000, 39000, 16, 6),
            ("TEC-002", "Audífonos inalámbricos", 5, 42000, 89000, 5, 5),
            ("TEC-003", "Forro para celular", 5, 5500, 18000, 70, 15)
        };
        var productos = datosProductos.Select((p, i) => new Producto
        {
            Codigo = p.cod, Nombre = p.nombre, Categoria = cats[p.cat], Bodega = bodegas[i % 3 == 0 ? 1 : 0],
            PrecioCompra = p.compra, PrecioVenta = p.venta, StockActual = p.stock, StockMinimo = p.min,
            Unidad = "Unidad", Descripcion = $"{p.nombre} - producto de demostración"
        }).ToList();
        db.Productos.AddRange(productos);

        // ── Clientes ──
        var nombresClientes = new[] { "María Fernanda López", "Juan Sebastián Gómez", "Laura Daniela Martínez", "Carlos Andrés Ruiz", "Natalia Herrera", "Santiago Castro", "Paula Andrea Vargas", "Diego Alejandro Rojas", "Sofía Moreno", "Felipe Jiménez" };
        var ciudades = new[] { "Chía", "Bogotá", "Cajicá", "Zipaquirá", "Cota", "Sopó", "Tenjo" };
        var clientes = nombresClientes.Select((n, i) => new Cliente
        {
            TipoDocumento = "CC", Documento = (1000000000 + i * 734521).ToString(), Nombre = n,
            Telefono = $"3{rnd.Next(10, 25)}{rnd.Next(1000000, 9999999)}",
            Email = n.Split(' ')[0].ToLower().Replace("í", "i").Replace("á", "a").Replace("é", "e") + i + "@correo.com",
            Ciudad = ciudades[i % ciudades.Length], Direccion = $"Calle {rnd.Next(1, 150)} # {rnd.Next(1, 90)}-{rnd.Next(1, 99)}",
            FechaRegistro = hoy.AddDays(-rnd.Next(30, 400))
        }).ToList();
        db.Clientes.AddRange(clientes);

        // ── Proveedores ficticios ──
        var proveedores = new List<Proveedor>
        {
            new() { Nit = "900.111.222-3", RazonSocial = "Bisutería Andina S.A.S.", Contacto = "Gloria Pinzón", Telefono = "601 742 1100", Email = "ventas@bisuteriaandina.co", Ciudad = "Bogotá", Direccion = "Cl. 13 # 9-45", CondicionesPago = "Crédito 30 días", TiempoEntregaDias = 3, Calificacion = 4.6m },
            new() { Nit = "901.333.444-5", RazonSocial = "Distribuidora El Dorado Ltda.", Contacto = "Hernán Quintero", Telefono = "601 810 2233", Email = "pedidos@eldorado-dist.co", Ciudad = "Bogotá", Direccion = "Av. Jiménez # 4-12", CondicionesPago = "Contado", TiempoEntregaDias = 2, Calificacion = 4.1m },
            new() { Nit = "900.555.666-7", RazonSocial = "Cueros y Lonas del Valle", Contacto = "Marcela Ospina", Telefono = "602 390 4455", Email = "comercial@cuerosvalle.co", Ciudad = "Cali", Direccion = "Cra. 5 # 20-31", CondicionesPago = "Crédito 15 días", TiempoEntregaDias = 6, Calificacion = 4.3m },
            new() { Nit = "901.777.888-9", RazonSocial = "Belleza Natural Importaciones", Contacto = "Ricardo Salcedo", Telefono = "604 512 6677", Email = "contacto@bellezanatural.co", Ciudad = "Medellín", Direccion = "Cl. 10 Sur # 43-15", CondicionesPago = "Crédito 30 días", TiempoEntregaDias = 4, Calificacion = 3.9m },
            new() { Nit = "900.999.000-1", RazonSocial = "Papelería Mayorista Central", Contacto = "Liliana Rubio", Telefono = "601 333 8899", Email = "mayoristas@papeleriacentral.co", Ciudad = "Bogotá", Direccion = "Cra. 7 # 22-10", CondicionesPago = "Contado", TiempoEntregaDias = 1, Calificacion = 4.8m },
            new() { Nit = "901.246.135-2", RazonSocial = "TecnoImport Colombia S.A.S.", Contacto = "Óscar Beltrán", Telefono = "601 444 1212", Email = "ventas@tecnoimport.co", Ciudad = "Bogotá", Direccion = "Unilago Local 210", CondicionesPago = "Crédito 15 días", TiempoEntregaDias = 3, Calificacion = 4.0m },
            new() { Nit = "900.864.209-6", RazonSocial = "Casa Hogar Decoraciones", Contacto = "Patricia Mejía", Telefono = "601 676 5544", Email = "pedidos@casahogar.co", Ciudad = "Chía", Direccion = "Centro Comercial Fontanar", CondicionesPago = "Crédito 30 días", TiempoEntregaDias = 5, Calificacion = 4.4m }
        };
        db.Proveedores.AddRange(proveedores);

        // Qué vende cada proveedor (por categoría, con precios de referencia distintos).
        var proveedoresPorCategoria = new Dictionary<int, int[]>
        {
            [0] = new[] { 0, 1 }, [1] = new[] { 2, 1 }, [2] = new[] { 3, 1 },
            [3] = new[] { 4, 1 }, [4] = new[] { 6, 1 }, [5] = new[] { 5, 1 }
        };
        for (var i = 0; i < productos.Count; i++)
        {
            var catIndex = datosProductos[i].cat;
            foreach (var pi in proveedoresPorCategoria[catIndex])
            {
                var factor = 0.9m + (decimal)rnd.NextDouble() * 0.25m;
                db.ProveedorProductos.Add(new ProveedorProducto
                {
                    Proveedor = proveedores[pi], Producto = productos[i],
                    PrecioReferencia = Calculos.Redondear(productos[i].PrecioCompra * factor / 100) * 100
                });
            }
        }

        // ── Empleados con ficha completa ──
        var empleados = new List<Empleado>
        {
            Emp("1070123456", "Valentina", "Ríos Castaño", new DateTime(1998, 4, 12), "Femenino", 162, 55, "O+", "Asesora comercial", "Ventas", hoy.AddYears(-3).AddMonths(-2), 1600000, "Sura", "Chía"),
            Emp("1032456789", "Andrés Felipe", "Molina Duarte", new DateTime(1994, 9, 3), "Masculino", 176, 74, "A+", "Encargado de inventario", "Inventario y bodega", hoy.AddYears(-5).AddMonths(-7), 2100000, "Sanitas", "Cajicá"),
            Emp("1019876543", "Camila", "Torres Beltrán", new DateTime(1991, 1, 25), "Femenino", 168, 60, "B+", "Analista de talento humano", "Talento humano", hoy.AddYears(-4), 2600000, "Compensar", "Bogotá"),
            Emp("80123987", "Jorge Enrique", "Pardo Silva", new DateTime(1985, 11, 30), "Masculino", 172, 81, "O-", "Administrador de tienda", "Administración", hoy.AddYears(-8).AddMonths(-3), 3800000, "Nueva EPS", "Chía"),
            Emp("1072654321", "Daniela", "Suárez Peña", new DateTime(2001, 7, 18), "Femenino", 158, 52, "A-", "Asesora comercial", "Ventas", hoy.AddMonths(-10), 1450000, "Salud Total", "Zipaquirá"),
            Emp("1013579246", "Miguel Ángel", "Cárdenas Ruiz", new DateTime(1999, 2, 8), "Masculino", 180, 77, "O+", "Auxiliar de bodega", "Inventario y bodega", hoy.AddYears(-1).AddMonths(-4), 1423500, "Famisanar", "Cota"),
            Emp("52987654", "Luz Marina", "Gómez Arias", new DateTime(1979, 5, 22), "Femenino", 160, 66, "AB+", "Cajera", "Ventas", hoy.AddYears(-6).AddMonths(-1), 1700000, "Sura", "Chía"),
            Emp("1075246813", "Sebastián", "Navarro León", new DateTime(2003, 12, 1), "Masculino", 174, 68, "B-", "Aprendiz SENA - ventas", "Ventas", hoy.AddMonths(-5), 1067625, "Coosalud", "Tenjo"),
            Emp("1020304050", "Juliana", "Ramírez Ortiz", new DateTime(1996, 8, 14), "Femenino", 165, 58, "O+", "Auxiliar de compras", "Compras", hoy.AddYears(-2).AddMonths(-6), 1900000, "Aliansalud", "Sopó"),
            Emp("79654123", "Hernando", "Vega Lozano", new DateTime(1982, 3, 9), "Masculino", 169, 79, "A+", "Contador (externo)", "Administración", hoy.AddYears(-3), 2500000, "Sanitas", "Bogotá")
        };
        empleados[7].TipoContrato = "Aprendizaje"; empleados[7].Jornada = "Medio tiempo"; empleados[7].NivelEducativo = "Técnico";
        empleados[9].TipoContrato = "Prestación de servicios"; empleados[9].Jornada = "Por horas"; empleados[9].NivelEducativo = "Profesional"; empleados[9].Profesion = "Contador público";
        empleados[2].NivelEducativo = "Profesional"; empleados[2].Profesion = "Psicóloga";
        empleados[3].NivelEducativo = "Especialización"; empleados[3].Profesion = "Administrador de empresas";
        for (var i = 0; i < empleados.Count; i++)
        {
            var e = empleados[i];
            e.Arl = Listas.Arl[i % Listas.Arl.Length];
            e.FondoPensiones = Listas.FondosPension[i % Listas.FondosPension.Length];
            e.FondoCesantias = Listas.FondosCesantias[i % Listas.FondosCesantias.Length];
            e.CajaCompensacion = i % 3 == 0 ? "Comfacundi" : Listas.Cajas[i % Listas.Cajas.Length];
            e.EstadoCivil = Listas.EstadosCiviles[i % Listas.EstadosCiviles.Length];
            e.PersonasACargo = i % 4;
            e.Banco = Listas.Bancos[i % Listas.Bancos.Length];
            e.TipoCuenta = "Ahorros";
            e.NumeroCuenta = $"000-{rnd.Next(100000, 999999)}-{rnd.Next(10, 99)} (ficticia)";
            e.Telefono = $"3{rnd.Next(10, 25)}{rnd.Next(1000000, 9999999)}";
            e.Email = $"{e.Nombres.Split(' ')[0].ToLower().Replace("á", "a").Replace("é", "e").Replace("í", "i")}.{e.Apellidos.Split(' ')[0].ToLower().Replace("í", "i").Replace("é", "e").Replace("á", "a").Replace("ú", "u")}@markerp.demo";
            e.Direccion = $"Cra. {rnd.Next(1, 80)} # {rnd.Next(1, 150)}-{rnd.Next(1, 99)}";
            e.TallaCamisa = e.Sexo == "Femenino" ? new[] { "S", "M", "XS" }[i % 3] : new[] { "M", "L", "XL" }[i % 3];
            e.TallaPantalon = e.Sexo == "Femenino" ? new[] { "8", "10", "6" }[i % 3] : new[] { "32", "34", "30" }[i % 3];
            e.TallaCalzado = e.Sexo == "Femenino" ? new[] { "36", "37", "38" }[i % 3] : new[] { "40", "41", "42" }[i % 3];
            e.ContactoEmergenciaNombre = new[] { "Rosa Castaño", "Pedro Molina", "Inés Beltrán", "Clara Silva", "Mario Suárez", "Elena Ruiz", "Tomás Gómez", "Adriana León", "Fabio Ramírez", "Carmen Lozano" }[i];
            e.ContactoEmergenciaParentesco = new[] { "Madre", "Padre", "Madre", "Esposa", "Padre", "Madre", "Hijo", "Madre", "Padre", "Esposa" }[i];
            e.ContactoEmergenciaTelefono = $"3{rnd.Next(10, 25)}{rnd.Next(1000000, 9999999)}";
        }
        db.Empleados.AddRange(empleados);

        // Turnos de la semana actual
        var lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
        for (var d = 0; d < 6; d++)
        {
            for (var i = 0; i < empleados.Count - 1; i++)
            {
                if ((i + d) % 4 == 3) continue; // descanso
                var manana = (i + d) % 2 == 0;
                db.Turnos.Add(new Turno
                {
                    Empleado = empleados[i], Fecha = lunes.AddDays(d),
                    HoraInicio = manana ? new TimeSpan(7, 0, 0) : new TimeSpan(13, 0, 0),
                    HoraFin = manana ? new TimeSpan(15, 0, 0) : new TimeSpan(21, 0, 0),
                    Tipo = manana ? "Mañana" : "Tarde"
                });
            }
        }

        db.Novedades.AddRange(
            new Novedad { Empleado = empleados[4], Tipo = "Incapacidad", FechaInicio = hoy.AddDays(-1), FechaFin = hoy.AddDays(2), Descripcion = "Incapacidad médica general", Estado = "Aprobada" },
            new Novedad { Empleado = empleados[1], Tipo = "Vacaciones", FechaInicio = hoy.AddDays(12), FechaFin = hoy.AddDays(26), Descripcion = "Vacaciones del período anterior", Estado = "Aprobada" },
            new Novedad { Empleado = empleados[6], Tipo = "Permiso", FechaInicio = hoy.AddDays(3), FechaFin = hoy.AddDays(3), Descripcion = "Cita personal en la mañana", Estado = "Pendiente" });

        db.SaveChanges();

        // ── Ventas de los últimos 30 días ──
        var inventario = new InventarioService(db);
        var metodos = Listas.MetodosPago;
        var vendedores = new[] { "Valentina Ríos", "Daniela Suárez", "Luz Marina Gómez" };
        var numero = 1;
        for (var dia = 29; dia >= 0; dia--)
        {
            var horas = Enumerable.Range(0, rnd.Next(1, 5)).Select(_ => TimeSpan.FromMinutes(rnd.Next(9 * 60, 19 * 60))).OrderBy(h => h).ToList();
            foreach (var hora in horas)
            {
                var venta = new Venta
                {
                    Numero = Consecutivos.Formato("FV", numero++),
                    Fecha = hoy.AddDays(-dia).Add(hora),
                    Cliente = rnd.Next(0, 3) == 0 ? null : clientes[rnd.Next(clientes.Count)],
                    Vendedor = vendedores[rnd.Next(vendedores.Length)],
                    MetodoPago = metodos[rnd.Next(metodos.Length)]
                };
                var usados = new HashSet<int>();
                for (var l = 0; l < rnd.Next(1, 4); l++)
                {
                    var p = productos[rnd.Next(productos.Count)];
                    if (!usados.Add(p.Id)) continue;
                    var cant = rnd.Next(1, 3);
                    p.StockActual += cant; // la venta histórica no debe dejar stock negativo
                    inventario.Registrar(p, TiposMovimiento.Salida, cant, "Venta", venta.Numero, venta.Vendedor).Fecha = venta.Fecha;
                    venta.Detalles.Add(new DetalleVenta { Producto = p, Cantidad = cant, PrecioUnitario = p.PrecioVenta, Subtotal = cant * p.PrecioVenta });
                }
                var t = Calculos.TotalesVenta(venta.Detalles.Select(d => (d.Cantidad, d.PrecioUnitario)), 0, 19);
                (venta.Subtotal, venta.Descuento, venta.Iva, venta.Total) = t;
                db.Ventas.Add(venta);
            }
        }
        db.SaveChanges();

        // ── Cotización 1: adjudicada, con orden de compra recibida ──
        var sol1 = new SolicitudCotizacion
        {
            Numero = Consecutivos.Formato("SC", 1), Fecha = hoy.AddDays(-12), FechaLimite = hoy.AddDays(-8),
            Descripcion = "Reposición de accesorios para temporada", CreadaPor = "Andrés Molina", Estado = EstadosSolicitud.Adjudicada
        };
        sol1.Items.Add(new ItemSolicitud { Producto = productos[0], Cantidad = 20 });
        sol1.Items.Add(new ItemSolicitud { Producto = productos[1], Cantidad = 15 });
        foreach (var pi in new[] { 0, 1 })
            sol1.Envios.Add(new EnvioCotizacion { Proveedor = proveedores[pi], EmailDestino = proveedores[pi].Email!, FechaEnvio = sol1.Fecha });
        var c1a = CrearCotizacion(proveedores[0], sol1, new[] { 17500m, 21000m }, 3, sol1.Fecha.AddDays(2));
        var c1b = CrearCotizacion(proveedores[1], sol1, new[] { 16900m, 19800m }, 2, sol1.Fecha.AddDays(3));
        sol1.Cotizaciones.AddRange(new[] { c1a, c1b });
        EvaluadorCotizaciones.Puntuar(sol1.Cotizaciones);
        var ganadora1 = EvaluadorCotizaciones.Recomendada(sol1.Cotizaciones)!;
        foreach (var c in sol1.Cotizaciones) c.Estado = c == ganadora1 ? EstadosCotizacion.Seleccionada : EstadosCotizacion.Descartada;
        sol1.MotivoSeleccion = "Mejor puntaje: buen precio y proveedor con alta calificación.";
        sol1.AhorroEstimado = sol1.Cotizaciones.Max(c => c.Total) - ganadora1.Total;
        db.Solicitudes.Add(sol1);
        db.SaveChanges();
        sol1.CotizacionGanadoraId = ganadora1.Id;

        var oc1 = new OrdenCompra
        {
            Numero = Consecutivos.Formato("OC", 1), Fecha = hoy.AddDays(-7), Proveedor = ganadora1.Proveedor, Cotizacion = ganadora1,
            Estado = EstadosOrden.Recibida, CondicionesPago = ganadora1.CondicionesPago, FechaEntregaEstimada = hoy.AddDays(-4),
            FechaRecepcion = hoy.AddDays(-4), CreadaPor = "Andrés Molina", Observaciones = "Generada desde " + sol1.Numero
        };
        foreach (var d in ganadora1.Detalles)
            oc1.Detalles.Add(new DetalleOrdenCompra { Producto = d.Producto, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal });
        oc1.Subtotal = ganadora1.Subtotal; oc1.Iva = ganadora1.Iva; oc1.Total = ganadora1.Total;
        db.OrdenesCompra.Add(oc1);
        foreach (var d in oc1.Detalles)
        {
            d.Producto!.StockActual -= d.Cantidad; // el stock inicial ya incluye esta recepción
            inventario.Registrar(d.Producto, TiposMovimiento.Entrada, d.Cantidad, "Recepción de orden de compra", oc1.Numero, "Andrés Molina").Fecha = hoy.AddDays(-4);
        }

        // ── Cotización 2: en evaluación con 3 respuestas ──
        var sol2 = new SolicitudCotizacion
        {
            Numero = Consecutivos.Formato("SC", 2), Fecha = hoy.AddDays(-3), FechaLimite = hoy.AddDays(2),
            Descripcion = "Billeteras y set de esferos agotándose", CreadaPor = "Andrés Molina", Estado = EstadosSolicitud.EnEvaluacion
        };
        sol2.Items.Add(new ItemSolicitud { Producto = productos[6], Cantidad = 12 });
        sol2.Items.Add(new ItemSolicitud { Producto = productos[13], Cantidad = 30 });
        foreach (var pi in new[] { 1, 2, 4 })
            sol2.Envios.Add(new EnvioCotizacion { Proveedor = proveedores[pi], EmailDestino = proveedores[pi].Email!, FechaEnvio = sol2.Fecha });
        sol2.Cotizaciones.Add(CrearCotizacion(proveedores[1], sol2, new[] { 15800m, 7400m }, 2, hoy.AddDays(-2)));
        sol2.Cotizaciones.Add(CrearCotizacion(proveedores[2], sol2, new[] { 13900m, 7900m }, 6, hoy.AddDays(-2)));
        sol2.Cotizaciones.Add(CrearCotizacion(proveedores[4], sol2, new[] { 16500m, 6600m }, 1, hoy.AddDays(-1)));
        EvaluadorCotizaciones.Puntuar(sol2.Cotizaciones);
        db.Solicitudes.Add(sol2);

        // ── Cotización 3: recién enviada, sin respuestas ──
        var sol3 = new SolicitudCotizacion
        {
            Numero = Consecutivos.Formato("SC", 3), Fecha = hoy, FechaLimite = hoy.AddDays(5),
            Descripcion = "Audífonos inalámbricos para fin de año", CreadaPor = "Andrés Molina", Estado = EstadosSolicitud.Enviada
        };
        sol3.Items.Add(new ItemSolicitud { Producto = productos[18], Cantidad = 10 });
        foreach (var pi in new[] { 5, 1 })
            sol3.Envios.Add(new EnvioCotizacion { Proveedor = proveedores[pi], EmailDestino = proveedores[pi].Email!, FechaEnvio = hoy });
        db.Solicitudes.Add(sol3);

        db.Bitacora.Add(new Bitacora { Usuario = "sistema", Modulo = "Sistema", Accion = "Datos de demostración cargados" });
        db.SaveChanges();
    }

    private static Cotizacion CrearCotizacion(Proveedor prov, SolicitudCotizacion sol, decimal[] precios, int dias, DateTime fecha)
    {
        var c = new Cotizacion
        {
            Proveedor = prov, TiempoEntregaDias = dias, CondicionesPago = prov.CondicionesPago,
            ValidezDias = 15, FechaRecepcion = fecha, Observaciones = "Precios incluyen transporte a Chía."
        };
        for (var i = 0; i < sol.Items.Count; i++)
        {
            var it = sol.Items[i];
            c.Detalles.Add(new DetalleCotizacion { Producto = it.Producto, Cantidad = it.Cantidad, PrecioUnitario = precios[i], Subtotal = precios[i] * it.Cantidad });
        }
        c.Subtotal = c.Detalles.Sum(d => d.Subtotal);
        c.Iva = Calculos.Redondear(c.Subtotal * 0.19m);
        c.Total = c.Subtotal + c.Iva;
        return c;
    }

    private static Empleado Emp(string doc, string nombres, string apellidos, DateTime nac, string sexo, int estatura, decimal peso,
        string sangre, string cargo, string area, DateTime ingreso, decimal salario, string eps, string ciudad) => new()
    {
        TipoDocumento = "CC", NumeroDocumento = doc, Nombres = nombres, Apellidos = apellidos, FechaNacimiento = nac,
        Sexo = sexo, EstaturaCm = estatura, PesoKg = peso, TipoSangre = sangre, Cargo = cargo, Area = area,
        FechaIngreso = ingreso, Salario = salario, Eps = eps, Ciudad = ciudad, LugarNacimiento = ciudad,
        TipoContrato = "Término indefinido", Jornada = "Tiempo completo", NivelEducativo = "Bachiller"
    };
}
