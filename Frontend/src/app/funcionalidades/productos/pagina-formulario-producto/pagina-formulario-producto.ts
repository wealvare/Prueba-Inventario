import {
  afterNextRender,
  Component,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  numberAttribute,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Campo } from '../../../compartido/formularios/campo';
import { mensajeError } from '../../../compartido/formularios/mensaje-error';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { ServicioProductos } from '../servicios/servicio-productos';
import {
  cargarProducto,
  crearFormulario,
  crearLote,
  FormularioLote,
  leerSolicitud,
  normalizar,
  numerosRepetidos,
} from './formulario-producto';

@Component({
  imports: [Campo, ReactiveFormsModule, RouterLink],
  selector: 'app-pagina-formulario-producto',
  templateUrl: './pagina-formulario-producto.html',
})
export class PaginaFormularioProducto {
  private readonly servicio = inject(ServicioProductos);
  private readonly notificaciones = inject(ServicioNotificaciones);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly id = input(undefined, {
    transform: (valor?: string) => (valor === undefined ? undefined : numberAttribute(valor)),
  });

  protected readonly formulario = crearFormulario();
  protected readonly categorias = rxResource({ stream: () => this.servicio.listarCategorias() });
  protected readonly cargando = signal(false);
  protected readonly guardando = signal(false);
  protected readonly errorCarga = signal<string | null>(null);
  protected readonly errorGeneral = signal<string | null>(null);
  protected readonly hayConflicto = signal(false);
  private readonly erroresServidor = signal<Record<string, string[]>>({});

  constructor() {
    effect((alLimpiar) => {
      const id = this.id();

      if (id !== undefined) {
        const suscripcion = this.cargar(id);
        alLimpiar(() => suscripcion.unsubscribe());
      }
    });

    this.formulario.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.limpiarErrores());
  }

  protected get lotes() {
    return this.formulario.controls.lotes;
  }

  protected mensaje(control: AbstractControl, ruta: string): string | null {
    return mensajeError(control) ?? this.erroresServidor()[ruta]?.[0] ?? null;
  }

  protected mensajeNumeroLote(lote: FormularioLote, indice: number): string | null {
    const control = lote.controls.numeroLote;
    const repetido = numerosRepetidos(this.lotes.getRawValue()).has(normalizar(control.value));

    return (
      this.mensaje(control, `lotes[${indice}].numeroLote`) ??
      (repetido ? 'El número de lote está repetido.' : null)
    );
  }

  protected agregarLote(): void {
    this.lotes.push(crearLote());
    this.enfocar(`#numeroLote-${this.lotes.length - 1}`);
  }

  protected quitarLote(indice: number): void {
    this.lotes.removeAt(indice);
    this.enfocar('#agregar-lote');
  }

  protected recargar(): void {
    const id = this.id();

    if (id !== undefined) {
      this.cargar(id);
    }
  }

  protected guardar(): void {
    if (this.guardando()) {
      return;
    }

    this.limpiarErrores();

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.enfocar('[aria-invalid="true"]');
      return;
    }

    const id = this.id();
    const { activo, ...datos } = leerSolicitud(this.formulario);
    const peticion =
      id === undefined
        ? this.servicio.crear(datos)
        : this.servicio.actualizar(id, { ...datos, activo });

    this.guardando.set(true);
    peticion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: (producto) => {
        this.notificaciones.exito(id === undefined ? 'Producto creado.' : 'Producto actualizado.');
        void this.router.navigate(['/productos', producto.id]);
      },
      error: (error: ErrorApi) => this.mostrarError(error),
    });
  }

  private cargar(id: number) {
    this.cargando.set(true);
    this.errorCarga.set(null);

    return this.servicio
      .obtener(id)
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (producto) => {
          cargarProducto(this.formulario, producto);
          this.limpiarErrores();
        },
        error: (error: ErrorApi) => this.errorCarga.set(error.message),
      });
  }

  private mostrarError(error: ErrorApi): void {
    if (error.estado !== 400 && error.estado !== 409) {
      return;
    }

    this.erroresServidor.set(error.errores);
    this.errorGeneral.set(error.message);
    this.hayConflicto.set(error.estado === 409);

    if (Object.keys(error.errores).length > 0) {
      this.enfocar('[aria-invalid="true"]');
    }
  }

  private limpiarErrores(): void {
    this.erroresServidor.set({});
    this.errorGeneral.set(null);
    this.hayConflicto.set(false);
  }

  private enfocar(selector: string): void {
    afterNextRender(
      () => this.elemento.nativeElement.querySelector<HTMLElement>(selector)?.focus(),
      { injector: this.injector },
    );
  }
}
