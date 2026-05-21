import { Injectable, signal, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class SignalrService implements OnDestroy {
  readonly notifications = signal<string[]>([]);

  private readonly connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/notifications')
    .withAutomaticReconnect()
    .build();

  constructor() {
    this.connection.on('ReceiveNotification', (message: string) => {
      this.notifications.update((msgs) => [...msgs, message]);
    });

    this.connection
      .start()
      .catch((err) => console.error('SignalR connection error:', err));
  }

  ngOnDestroy(): void {
    this.connection.stop();
  }
}
