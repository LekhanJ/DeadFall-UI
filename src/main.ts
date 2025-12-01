import { Application, Assets, Container, Rectangle, Sprite, Texture, Ticker } from "pixi.js";
import { Player } from "./entities/player";
import { loadMap } from "./helper/map_loader";
import type { Input, PlayerMovement } from "./types/types";
import { Communicator } from "./helper/communicator";
import { Controls } from "./helper/controls";

let SCREEN_WIDTH: number = 800;
let SCREEN_HEIGHT: number = 600;

(async () => {
  const app = await createApplication();
  SCREEN_HEIGHT = app.screen.height;
  SCREEN_WIDTH = app.screen.width;
  await loadMap(app);
  
  const communicator = new Communicator("ws://localhost:3000");
  const controller = new Controls();
  
  const players = new Map<string, Player>();
  let mySessionId: string | null = null;

  communicator.on("connected", (data) => {
    mySessionId = data.sessionId;
    console.log("My session ID:", mySessionId);
  });

  communicator.on("playerMove", (data) => {
    if (!data.players) return;

    for (const [sessionId, playerData] of Object.entries(data.players)) {
      const pos = playerData as { x: number; y: number };
      
      if (!players.has(sessionId)) {
        createPlayer(sessionId).then(player => {
          players.set(sessionId, player);
          app.stage.addChild(player);
          player.setPosition(pos.x, pos.y);
        });
      } else {
        const player = players.get(sessionId);
        if (player) {
          player.setPosition(pos.x, pos.y);
        }
      }
    }

    const serverPlayerIds = new Set(Object.keys(data.players));
    for (const [sessionId, player] of players.entries()) {
      if (!serverPlayerIds.has(sessionId)) {
        app.stage.removeChild(player);
        players.delete(sessionId);
        console.log("Player left:", sessionId);
      }
    }
  });

  app.ticker.add(() => {
    const playerMovement: PlayerMovement = {
      x: 0,
      y: 0,
      inputs: controller.inputs,
    };
    communicator.send("playerMove", playerMovement);
  });
})();

async function createApplication(): Promise<Application> {
  const app = new Application();
  await app.init({ resizeTo: window });
  document.body.appendChild(app.canvas);
  return app;
}

async function createPlayer(sessionId: string): Promise<Player> {
  const tex = await Assets.load("public/player.png");
  const player = new Player(sessionId);
  const sprite = Sprite.from(tex);
  sprite.anchor.set(0.5);
  player.addChild(sprite);
  player.setPosition(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2);
  player.scale.set(0.25);
  return player;
}