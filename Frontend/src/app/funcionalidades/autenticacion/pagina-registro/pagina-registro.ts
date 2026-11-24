import { afterNextRender, Component, ElementRef, inject, Injector, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Campo } from '../../../compartido/formularios/campo';
import { mensajeError } from '../../../compartido/formularios/mensaje-error';
import {
  contrasenaSegura,
  contrasenasIguales,
  VALIDADORES_CORREO,
  VALIDADORES_NOMBRE_USUARIO,
} from '../../../compartido/formularios/validadores-usuario';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { SolicitudRegistro } from '../../../nucleo/modelos/sesion';
import {
  RUTA_INICIO_SESION,
  ServicioAutenticacion,
} from '../../../nucleo/servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';

@Component({
  imports: [Campo, ReactiveFormsModule, RouterLink],
  selector: 'app-pagina-registro',
  templateUrl: './pagina-registro.html',
})
export class PaginaRegistro {
  private readonly autenticacion = inject(ServicioAutenticacion);
  private readonly notificaciones = inject(ServicioNotificaciones);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly enviando = signal(false);
  protected readonly errorServidor = signal<string | null>(null);
  private readonly erroresCampos = signal<Record<string, string[]>>({});
  protected readonly formulario = inject(NonNullableFormBuilder).group(
    {
      nombreUsuario: ['', VALIDADORES_NOMBRE_USUARIO],
      correo: ['', VALIDADORES_CORREO],
      contrasena: ['', [Validators.required, contrasenaSegura]],
      confirmacion: ['', Validators.required],
    },
    { validators: contrasenasIguales },
  );

  constructor() {
    this.formulario.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.limpiarErrores());
  }

  protected mensaje(campo: keyof SolicitudRegistro): string | null {
    return (
      mensajeError(this.formulario.controls[campo]) ?? this.erroresCampos()[campo]?.[0] ?? null
    );
  }

  protected mensajeConfirmacion(): string | null {
    const { confirmacion } = this.formulario.controls;
    const distintas = confirmacion.touched && this.formulario.hasError('contrasenasDistintas');
    return mensajeError(confirmacion) ?? (distintas ? 'Las contraseñas no coinciden.' : null);
  }

  protected registrar(): void {
    if (this.enviando()) {
      return;
    }

    this.limpiarErrores();

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.enfocarError();
      return;
    }

    const { nombreUsuario, correo, contrasena } = this.formulario.getRawValue();

    this.enviando.set(true);
    this.autenticacion
      .registrar({ nombreUsuario: nombreUsuario.trim(), correo: correo.trim(), contrasena })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          this.notificaciones.exito('Cuenta creada. Ya puedes iniciar sesión.');
          void this.router.navigateByUrl(RUTA_INICIO_SESION);
        },
        error: (error: ErrorApi) => this.mostrarError(error),
      });
  }

  private mostrarError(error: ErrorApi): void {
    if (![400, 409, 429].includes(error.estado)) {
      return;
    }

    this.erroresCampos.set(error.errores);
    this.errorServidor.set(error.message);

    if (Object.keys(error.errores).length > 0) {
      this.enfocarError();
    }
  }

  private limpiarErrores(): void {
    this.erroresCampos.set({});
    this.errorServidor.set(null);
  }

  private enfocarError(): void {
    afterNextRender(
      () =>
        this.elemento.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
