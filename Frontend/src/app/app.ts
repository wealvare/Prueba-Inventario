import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notificaciones } from './compartido/notificaciones/notificaciones';

@Component({
  imports: [Notificaciones, RouterOutlet],
  selector: 'app-root',
  template: `
    <router-outlet />
    <app-notificaciones />
  `,
})
export class App {}
