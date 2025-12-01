import { Application, Assets, Rectangle, Texture } from "pixi.js";
import mapData from "../../public/deadfall_map.json";
import { CompositeTilemap, Tilemap } from "@pixi/tilemap";

const TILE_SIZE = 256;
const tileTextures = {};


export async function loadMap(app: Application) {
    const tileset = await Assets.load("public/deadfall_map_sheet.png");
    const tilesetCols: number = tileset.width / TILE_SIZE;
    const tilesetRows: number = tileset.height / TILE_SIZE;

    createTileTextures(tileset, tilesetCols, tilesetRows);
    for (const layer of mapData.layers) {
        if (layer.type === "tilelayer") {
            const tileLayer = renderTileLayer(layer);
            app.stage.addChild(tileLayer);
        }
    }
}

function createTileTextures(tilesetTexture: any, columns: number, rows: number) {
  let id = 1;

  for (let y = 0; y < rows; y++) {
    for (let x = 0; x < columns; x++) {
      tileTextures[id] = new Texture({
        source: tilesetTexture.source,
        frame: new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE),
      });
      console.log(tileTextures[id]);
      
      id++;
    }
  }
}

function renderTileLayer(layer) {
  const tilemap = new CompositeTilemap();

  const width = layer.width;
  const height = layer.height;

  for (let i = 0; i < layer.data.length; i++) {
    const tileId = layer.data[i];
    if (tileId === 0) continue; 

    const x = (i % width) * TILE_SIZE;
    const y = Math.floor(i / width) * TILE_SIZE;

    tilemap.tile(tileTextures[tileId], x, y);
  }

  return tilemap;
}

