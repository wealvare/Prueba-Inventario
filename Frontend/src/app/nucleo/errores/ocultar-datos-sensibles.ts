const OCULTO = '[oculto]';

const PATRONES: RegExp[] = [
  /\bBearer\s+\S+/gi,
  /\b(contrase(n|ñ|%C3%B1)a|password|passwd|clave|token|secret|authorization)["']?\s*[:=]\s*("[^"]*"?|'[^']*'?|[^\s,;]+)/gi,
  /"[^"]*"/g,
  /[?#][^\s=]+=\S*/g,
  /[^\s@"'<>(),;:/]+(@|%40)[^\s@"'<>(),;:/]+/gi,
  /eyJ[\w-]+(\.[\w-]+)*/g,
  /[\w-]{32,}(\.[\w-]+)*/g,
  /\d{7,}/g,
];

export function ocultarDatosSensibles(texto: string): string {
  return PATRONES.reduce(
    (actual, patron) => actual.replace(patron, OCULTO),
    texto.replace(/[\p{C}\p{Zl}\p{Zp}]+/gu, ' '),
  ).trim();
}
