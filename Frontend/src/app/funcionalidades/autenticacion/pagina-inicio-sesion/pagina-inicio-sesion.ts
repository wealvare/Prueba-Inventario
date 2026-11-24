import { Component, inject, input, signal } from '@angular/core';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { Campo } from '../../../compartido/formularios/campo';
import { mensajeError } from '../../../compartido/formularios/mensaje-error';
import { textoRequerido } from '../../../compartido/formularios/validadores';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { rutaSegura } from '../ruta-segura';

@Component({
  imports: [Campo, ReactiveFormsModule],
  selector: 'app-pagina-inicio-sesion',
  templateUrl: './pagina-inicio-sesion.html',
})
export class PaginaInicioSesion {
  private readonly autenticacion = inject(ServicioAutenticacion);
  private readonly router = inject(Router);

  readonly retorno = input<string>();

  protected readonly enviando = signal(false);
  protected readonly errorServidor = signal<string | null>(null);
  protected readonly formulario = inject(NonNullableFormBuilder).group({
    nombreUsuario: ['', textoRequerido],
    contrasena: ['', textoRequerido],
  });

  protected mensaje(control: FormControl<string>): string | null {
    return mensajeError(control);
  }

  protected iniciarSesion(): void {
    if (this.enviando()) {
      return;
    }

    this.errorServidor.set(null);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.autenticacion
      .iniciarSesion(this.formulario.getRawValue())
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(rutaSegura(this.retorno())),
        error: (error: ErrorApi) => {
          this.formulario.controls.contrasena.reset();
          this.errorServidor.set(error.message);
        },
      });
  }
}
