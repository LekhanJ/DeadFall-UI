import { Container, type Sprite } from "pixi.js";

export class Player extends Container {
    name: string;

    constructor(name: string) {
        super();
        this.name = name;
    }

    setPosition(x: number, y: number) {
        this.x = x;
        this.y = y;
    }
}