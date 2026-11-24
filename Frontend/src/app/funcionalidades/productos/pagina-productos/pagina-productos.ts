import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { EtiquetaEstado } from '../../../compartido/etiqueta-estado/etiqueta-estado';
import { Campo } from '../../../compartido/formularios/campo';
import { Paginador } from '../../../compartido/paginador/paginador';
import { EstadoProductos, FiltroProductos, ProductoResumen } from '../modelos/producto';
import { ServicioProductos } from '../servicios/servicio-productos';
import { ESTADOS, escribirFiltro, FILTRO_INICIAL, leerFiltro, TAMANOS_PAGINA } from './filtro-url';

const RETARDO_MS = 400;

@Component({
  imports: [
    Campo,
    DecimalPipe,
    EtiquetaEstado,
    NgTemplateOutlet,
    Paginador,
    ReactiveFormsModule,
    RouterLink,
  ],
  selector: 'app-pagina-productos',
  templateUrl: './pagina-productos.html',
})
export class PaginaProductos {
  private readonly servicio = inject(ServicioProductos);
  private readonly router = inject(Router);
  private readonly ruta = inject(ActivatedRoute);
  private readonly parametros = toSignal(this.ruta.queryParamMap, { requireSync: true });
  private ultimoEnviado = '';

  protected readonly estados = ESTADOS;
  protected readonly tamanos = TAMANOS_PAGINA;
  protected readonly errorPrecios = signal<string | null>(null);

  protected readonly filtro = computed(() => leerFiltro(this.parametros()), {
    equal: (anterior, actual) => JSON.stringify(anterior) === JSON.stringify(actual),
  });

  protected readonly productos = rxResource({
    params: () => this.filtro(),
    stream: ({ params }) => this.servicio.listar(params),
  });

  protected readonly formulario = new FormGroup({
    busqueda: new FormControl('', { nonNullable: true }),
    categoria: new FormControl('', { nonNullable: true }),
    estado: new FormControl<EstadoProductos>('Activos', { nonNullable: true }),
    precioMinimo: new FormControl<number | null>(null, Validators.min(0)),
    precioMaximo: new FormControl<number | null>(null, Validators.min(0)),
    tamanoPagina: new FormControl(FILTRO_INICIAL.tamanoPagina, { nonNullable: true }),
  });

  constructor() {
    effect(() => {
      const filtro = this.filtro();

      if (JSON.stringify(filtro) !== this.ultimoEnviado) {
        this.formulario.patchValue(filtro, { emitEvent: false });
        this.errorPrecios.set(null);
      }

      this.ultimoEnviado = '';
    });

    effect(() => {
      if (!this.productos.hasValue()) {
        return;
      }

      const { pagina, totalPaginas } = this.productos.value();

      if (totalPaginas > 0 && pagina > totalPaginas) {
        this.irAPagina(totalPaginas);
      }
    });

    this.formulario.valueChanges
      .pipe(debounceTime(RETARDO_MS), takeUntilDestroyed())
      .subscribe(() => this.aplicarFiltros());
  }

  protected irAPagina(pagina: number): void {
    this.navegar({ ...this.filtro(), pagina });
  }

  protected limpiar(): void {
    this.formulario.reset(FILTRO_INICIAL);
  }

  private aplicarFiltros(): void {
    const valores = this.formulario.getRawValue();
    const { precioMinimo, precioMaximo } = valores;

    if (this.formulario.invalid) {
      this.errorPrecios.set('Los precios no pueden ser negativos.');
      return;
    }

    if (precioMinimo !== null && precioMaximo !== null && precioMaximo < precioMinimo) {
      this.errorPrecios.set('El precio máximo no puede ser menor que el mínimo.');
      return;
    }

    this.errorPrecios.set(null);
    this.navegar({
      ...valores,
      busqueda: valores.busqueda.trim(),
      categoria: valores.categoria.trim(),
      pagina: 1,
    });
  }

  private navegar(filtro: FiltroProductos): void {
    this.ultimoEnviado = JSON.stringify(leerOrden(filtro));
    void this.router.navigate([], {
      relativeTo: this.ruta,
      queryParams: escribirFiltro(filtro),
      replaceUrl: true,
    });
  }
}

function leerOrden(filtro: FiltroProductos): FiltroProductos {
  const { pagina, tamanoPagina, busqueda, categoria, estado, precioMinimo, precioMaximo } = filtro;
  return { pagina, tamanoPagina, busqueda, categoria, estado, precioMinimo, precioMaximo };
}
