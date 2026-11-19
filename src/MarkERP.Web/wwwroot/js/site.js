// ── Temas: se guardan en el navegador ──
(function () {
  const html = document.documentElement;
  function marcar(t) {
    document.querySelectorAll('.temas button').forEach(b => b.classList.toggle('activo', b.dataset.t === t));
  }
  let guardado = null;
  try { guardado = localStorage.getItem('markerp-tema'); } catch (e) { }
  if (guardado) html.dataset.theme = guardado;
  document.addEventListener('DOMContentLoaded', () => {
    marcar(html.dataset.theme);
    document.querySelectorAll('.temas button').forEach(b => b.addEventListener('click', () => {
      html.dataset.theme = b.dataset.t;
      try { localStorage.setItem('markerp-tema', b.dataset.t); } catch (e) { }
      marcar(b.dataset.t);
    }));
    const menu = document.querySelector('.btn-menu');
    if (menu) menu.addEventListener('click', () => document.querySelector('.sidebar').classList.toggle('abierta'));
    // Los avisos se ocultan solos
    setTimeout(() => document.querySelectorAll('.aviso.ok').forEach(a => { a.style.transition = 'opacity .6s'; a.style.opacity = '0'; setTimeout(() => a.remove(), 600); }), 5000);
    // Confirmación para acciones delicadas
    document.querySelectorAll('form[data-confirmar]').forEach(f => f.addEventListener('submit', ev => {
      if (!confirm(f.dataset.confirmar)) ev.preventDefault();
    }));
  });
})();

// ── Formato de moneda colombiana ──
window.moneda = v => '$' + Math.round(v).toLocaleString('es-CO');

// ── Tabla de líneas reutilizable (ventas, cotizaciones, órdenes) ──
// opciones: { tabla, plantilla, prefijo, alCambiar }
window.LineasDinamicas = function (op) {
  const tbody = document.querySelector(op.tabla + ' tbody');
  const plantilla = document.querySelector(op.plantilla);
  function reindexar() {
    [...tbody.querySelectorAll('tr')].forEach((tr, i) => {
      tr.querySelectorAll('[data-campo]').forEach(inp => inp.name = `${op.prefijo}[${i}].${inp.dataset.campo}`);
    });
    if (op.alCambiar) op.alCambiar(tbody);
  }
  function agregar(valores) {
    const fila = plantilla.content.firstElementChild.cloneNode(true);
    tbody.appendChild(fila);
    if (valores) Object.entries(valores).forEach(([k, v]) => { const el = fila.querySelector(`[data-campo="${k}"]`); if (el) el.value = v; });
    fila.querySelectorAll('select, input').forEach(el => el.addEventListener('input', reindexar));
    fila.querySelector('.quitar')?.addEventListener('click', () => { fila.remove(); reindexar(); });
    reindexar();
    return fila;
  }
  return { agregar, reindexar, tbody };
};
