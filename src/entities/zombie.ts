import { Container } from "pixi.js";

export class Zombie extends Container {
    setPosition(x: number, y: number) {
        this.x = x;
        this.y = y;
    }
}