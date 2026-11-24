import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { rutaSegura } from '../ruta-segura';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-pagina-inicio-sesion',
  templateUrl: './pagina-inicio-sesion.html',
})
export class PaginaInicioSesion {
  private readonly autenticacion = inject(ServicioAutenticacion);
  private readonly router = inject(Router);

  readonly retorno = input<string>();

  protected readonly enviando = signal(false);
  protected readonly faltanDatos = signal(false);
  protected readonly formulario = inject(NonNullableFormBuilder).group({
    nombreUsuario: ['', Validators.required],
    contrasena: ['', Validators.required],
  });

  protected iniciarSesion(): void {
    this.faltanDatos.set(this.formulario.invalid);

    if (this.formulario.invalid || this.enviando()) {
      return;
    }

    this.enviando.set(true);
    this.autenticacion
      .iniciarSesion(this.formulario.getRawValue())
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(rutaSegura(this.retorno())),
        error: () => this.formulario.controls.contrasena.reset(),
      });
  }
}
