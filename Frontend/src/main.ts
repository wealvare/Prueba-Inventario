import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig).catch((error) => {
  console.error('No se pudo iniciar la aplicación.', error);

  const aviso = document.createElement('p');
  aviso.setAttribute('role', 'alert');
  aviso.textContent =
    'No se pudo iniciar la aplicación. Recarga la página para intentarlo de nuevo.';
  document.body.replaceChildren(aviso);
});
