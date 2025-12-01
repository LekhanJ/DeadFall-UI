import type { PlayerMovement } from "../types/types";

export class Communicator {
  websocket: WebSocket;
  private listeners: Record<string, ((data: any) => void)[]> = {};

  constructor(host: string = "ws://localhost:3000") {
    this.websocket = this.createConnection(host);
  }

  private createConnection(url: string): WebSocket {
    const ws = new WebSocket(url);

    ws.onopen = () => console.log("WebSocket connected");

    ws.onmessage = (event) => {
      const msg = JSON.parse(event.data);
      const type = msg.type;

      if (this.listeners[type]) {
        this.listeners[type].forEach((cb) => cb(msg));
      }
    };

    ws.onclose = () => console.log("WebSocket closed");

    return ws;
  }

  send(type: string, payload: any = {}) {
    if (this.websocket.readyState === WebSocket.OPEN) {
      this.websocket.send(JSON.stringify({ type, ...payload }));
    }
  }

  on(type: string, callback: (data: any) => void) {
    if (!this.listeners[type]) this.listeners[type] = [];
    this.listeners[type].push(callback);
  }

  destroy() {
    this.websocket.close();
  }
}
