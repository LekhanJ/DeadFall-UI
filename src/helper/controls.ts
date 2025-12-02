export class Controls {
    inputs: [boolean, boolean, boolean, boolean]; 
    
    constructor() {
        this.inputs = [false, false, false, false];
        this.addKeyboardListeners();
    }

    private addKeyboardListeners() {
        window.addEventListener("keydown", (event) => {
            const key = event.key.toLowerCase();

            if (["w", "a", "s", "d"].includes(key)) {
                event.preventDefault();
            }

            switch (key) {
                case "w":
                    this.inputs[0] = true;
                    break;
                case "s":
                    this.inputs[1] = true;
                    break;
                case "a":
                    this.inputs[2] = true;
                    break;
                case "d":
                    this.inputs[3] = true;
                    break;
            }
        });

        window.addEventListener("keyup", (event) => {
            const key = event.key.toLowerCase();

            switch (key) {
                case "w":
                    this.inputs[0] = false;
                    break;
                case "s":
                    this.inputs[1] = false;
                    break;
                case "a":
                    this.inputs[2] = false;
                    break;
                case "d":
                    this.inputs[3] = false;
                    break;
            }
        });
    }
}
