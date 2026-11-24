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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ErrorCarga } from '../../../compartido/error-carga/error-carga';
import { Campo } from '../../../compartido/formularios/campo';
import { mensajeError } from '../../../compartido/formularios/mensaje-error';
import {
  contrasenaSegura,
  VALIDADORES_CORREO,
  VALIDADORES_NOMBRE_USUARIO,
} from '../../../compartido/formularios/validadores-usuario';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { Rol } from '../../../nucleo/modelos/sesion';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { ServicioUsuarios } from '../servicios/servicio-usuarios';

@Component({
  imports: [Campo, ErrorCarga, ReactiveFormsModule, RouterLink],
  selector: 'app-pagina-formulario-usuario',
  templateUrl: './pagina-formulario-usuario.html',
})
export class PaginaFormularioUsuario {
  private readonly servicio = inject(ServicioUsuarios);
  private readonly notificaciones = inject(ServicioNotificaciones);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly id = input(undefined, {
    transform: (valor?: string) => (valor === undefined ? undefined : numberAttribute(valor)),
  });

  protected readonly roles: readonly Rol[] = ['Consulta', 'Administrador'];
  protected readonly cargando = signal(false);
  protected readonly guardando = signal(false);
  protected readonly errorCarga = signal<ErrorApi | null>(null);
  protected readonly errorGeneral = signal<string | null>(null);
  private readonly erroresServidor = signal<Record<string, string[]>>({});

  protected readonly formulario = new FormGroup({
    nombreUsuario: new FormControl('', {
      nonNullable: true,
      validators: VALIDADORES_NOMBRE_USUARIO,
    }),
    correo: new FormControl('', { nonNullable: true, validators: VALIDADORES_CORREO }),
    contrasena: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, contrasenaSegura],
    }),
    rol: new FormControl<Rol>('Consulta', { nonNullable: true }),
    activo: new FormControl(true, { nonNullable: true }),
  });

  constructor() {
    effect((alLimpiar) => {
      const id = this.id();
      const { contrasena } = this.formulario.controls;

      contrasena.setValidators(
        id === undefined ? [Validators.required, contrasenaSegura] : contrasenaSegura,
      );
      contrasena.updateValueAndValidity({ emitEvent: false });

      if (id !== undefined) {
        const suscripcion = this.cargar(id);
        alLimpiar(() => suscripcion.unsubscribe());
      }
    });

    this.formulario.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.limpiarErrores());
  }

  protected mensaje(campo: keyof typeof this.formulario.controls): string | null {
    return (
      mensajeError(this.formulario.controls[campo]) ?? this.erroresServidor()[campo]?.[0] ?? null
    );
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
      this.enfocarError();
      return;
    }

    const id = this.id();
    const { nombreUsuario, correo, contrasena, rol, activo } = this.formulario.getRawValue();
    const datos = { nombreUsuario: nombreUsuario.trim(), correo: correo.trim(), rol };
    const peticion =
      id === undefined
        ? this.servicio.crear({ ...datos, contrasena })
        : this.servicio.actualizar(id, { ...datos, activo, contrasena: contrasena || null });

    this.guardando.set(true);
    peticion.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: () => {
        this.notificaciones.exito(id === undefined ? 'Usuario creado.' : 'Usuario actualizado.');
        void this.router.navigateByUrl('/usuarios');
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
        next: ({ nombreUsuario, correo, rol, activo }) =>
          this.formulario.reset({ nombreUsuario, correo, rol, activo, contrasena: '' }),
        error: (error: ErrorApi) => this.errorCarga.set(error),
      });
  }

  private mostrarError(error: ErrorApi): void {
    if (error.estado !== 400 && error.estado !== 409) {
      return;
    }

    this.erroresServidor.set(error.errores);
    this.errorGeneral.set(error.message);

    if (Object.keys(error.errores).length > 0) {
      this.enfocarError();
    }
  }

  private limpiarErrores(): void {
    this.erroresServidor.set({});
    this.errorGeneral.set(null);
  }

  private enfocarError(): void {
    afterNextRender(
      () =>
        this.elemento.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
