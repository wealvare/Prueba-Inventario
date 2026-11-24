import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import {
  dosDecimales,
  entero,
  mayorQueCero,
  textoRequerido,
} from '../../../compartido/formularios/validadores';
import { Lote, Producto, SolicitudActualizarProducto } from '../modelos/producto';

const PRECIO_MAXIMO = 9_999_999_999_999.99;
const CANTIDAD_MAXIMA = 2_147_483_647;

export type FormularioLote = FormGroup<{
  id: FormControl<number | null>;
  numeroLote: FormControl<string>;
  fechaIngreso: FormControl<string>;
  precio: FormControl<number | null>;
  cantidad: FormControl<number | null>;
}>;

type FormularioProducto = FormGroup<{
  codigo: FormControl<string>;
  nombre: FormControl<string>;
  descripcion: FormControl<string>;
  categoria: FormControl<string>;
  activo: FormControl<boolean>;
  lotes: FormArray<FormularioLote>;
}>;

export function crearFormulario(): FormularioProducto {
  return new FormGroup({
    codigo: texto(30),
    nombre: texto(120),
    descripcion: new FormControl('', { nonNullable: true, validators: Validators.maxLength(500) }),
    categoria: texto(80),
    activo: new FormControl(true, { nonNullable: true }),
    lotes: new FormArray<FormularioLote>([], lotesSinRepetir),
  });
}

export function crearLote(lote?: Lote): FormularioLote {
  return new FormGroup({
    id: new FormControl(lote?.id ?? null),
    numeroLote: texto(50, lote?.numeroLote),
    fechaIngreso: new FormControl(lote?.fechaIngreso ?? hoy(), {
      nonNullable: true,
      validators: Validators.required,
    }),
    precio: new FormControl(lote?.precio ?? null, [
      Validators.required,
      mayorQueCero,
      dosDecimales,
      Validators.max(PRECIO_MAXIMO),
    ]),
    cantidad: new FormControl(lote?.cantidad ?? null, [
      Validators.required,
      entero,
      Validators.min(0),
      Validators.max(CANTIDAD_MAXIMA),
    ]),
  });
}

export function cargarProducto(formulario: FormularioProducto, producto: Producto): void {
  formulario.controls.lotes.clear({ emitEvent: false });
  producto.lotes.forEach((lote) => formulario.controls.lotes.push(crearLote(lote)));
  formulario.patchValue({
    codigo: producto.codigo,
    nombre: producto.nombre,
    descripcion: producto.descripcion,
    categoria: producto.categoria,
    activo: producto.activo,
  });
  formulario.markAsUntouched();
}

export function leerSolicitud(formulario: FormularioProducto): SolicitudActualizarProducto {
  const valores = formulario.getRawValue();

  return {
    codigo: valores.codigo.trim(),
    nombre: valores.nombre.trim(),
    descripcion: valores.descripcion.trim(),
    categoria: valores.categoria.trim(),
    activo: valores.activo,
    lotes: valores.lotes.map((lote) => ({
      id: lote.id,
      numeroLote: lote.numeroLote.trim(),
      fechaIngreso: lote.fechaIngreso,
      precio: lote.precio ?? 0,
      cantidad: lote.cantidad ?? 0,
    })),
  };
}

export function numerosRepetidos(lotes: readonly { numeroLote: string }[]): Set<string> {
  const numeros = lotes
    .map((lote) => normalizar(lote.numeroLote))
    .filter((numero) => numero !== '');
  return new Set(numeros.filter((numero, indice) => numeros.indexOf(numero) !== indice));
}

export function normalizar(numeroLote: string): string {
  return numeroLote.trim().toLowerCase();
}

function lotesSinRepetir(control: AbstractControl): ValidationErrors | null {
  const lotes = (control as FormArray<FormularioLote>).getRawValue();
  return numerosRepetidos(lotes).size > 0 ? { lotesRepetidos: true } : null;
}

function texto(longitudMaxima: number, valor = ''): FormControl<string> {
  return new FormControl(valor, {
    nonNullable: true,
    validators: [textoRequerido, Validators.maxLength(longitudMaxima)],
  });
}

function hoy(): string {
  const fecha = new Date();
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}
