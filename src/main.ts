import { Application, Assets, Container, Sprite, Ticker } from "pixi.js";
import { Player } from "./entities/player";

const websocket = new WebSocket("ws://localhost:3000");

websocket.onopen = () => {
  console.log("WebSocket connection established");
};

(async () => {
  const app = new Application();
  await app.init({ resizeTo: window });
  document.body.appendChild(app.canvas);

  const keys: Record<string, boolean> = {};
  window.addEventListener("keydown", (e) => {
    keys[e.key] = true;
  });
  window.addEventListener("keyup", (e) => {
    keys[e.key] = false;
  });

  const tex = await Assets.load("public/vite.svg");
  const player = new Player("Player");
  const sprite = Sprite.from(tex);
  sprite.anchor.set(0.5);
  player.addChild(sprite);
  player.setPosition(app.screen.width / 2, app.screen.height / 2);

  app.stage.addChild(player);

  let position = {
    x: player.x,
    y: player.y,
  };

  websocket.onmessage = (event) => {
    const newPosition = JSON.parse(event.data);
    console.log(newPosition);

    player.x = newPosition.x;
    player.y = newPosition.y;

    position.x = player.x;
    position.y = player.y;
  };

  app.ticker.add(() => {
    if (websocket.readyState === WebSocket.OPEN) {
      const inputs = [];

      if (keys["w"]) inputs.push("up");
      if (keys["s"]) inputs.push("down");
      if (keys["a"]) inputs.push("left");
      if (keys["d"]) inputs.push("right");

      websocket.send(
        JSON.stringify({
          ...position,
          inputs,
        })
      );
    }
  });
})();
