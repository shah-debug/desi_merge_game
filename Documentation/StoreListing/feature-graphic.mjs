// Google Play feature graphic (1024 x 500) for Desi Merge, drawn from the game's own art and fonts.
import { createCanvas, loadImage, GlobalFonts } from "@napi-rs/canvas";
import { writeFileSync } from "node:fs";

const ART = "C:/Users/HP/DesiMerge/Assets/_Project/Art";
const OUT = process.argv[2];
GlobalFonts.registerFromPath(`${ART}/Fonts/LilitaOne-Regular.ttf`, "Lilita");
GlobalFonts.registerFromPath(`${ART}/Fonts/Poppins-SemiBold.ttf`, "Poppins");

const W = 1024, H = 500;
const c = createCanvas(W, H);
const g = c.getContext("2d");

// Pink sunburst like the app icon.
const bg = g.createRadialGradient(330, 250, 30, 330, 250, 760);
bg.addColorStop(0, "#FF4FAE");
bg.addColorStop(1, "#8E0C52");
g.fillStyle = bg;
g.fillRect(0, 0, W, H);
g.save();
g.translate(330, 250);
g.fillStyle = "rgba(255, 182, 39, 0.16)";
for (let i = 0; i < 24; i++) {
  g.beginPath();
  g.moveTo(0, 0);
  g.arc(0, 0, 1200, (i * 2 * Math.PI) / 24, ((i + 0.5) * 2 * Math.PI) / 24);
  g.closePath();
  g.fill();
}
g.restore();

// Truck-art border strips top and bottom.
const strip = await loadImage(`${ART}/UI/Game/BorderStrip.png`);
const stripH = 26;
const stripW = (strip.width * stripH) / strip.height;
for (let x = 0; x < W; x += stripW) {
  g.drawImage(strip, x, 0, stripW, stripH);
  g.drawImage(strip, x, H - stripH, stripW, stripH);
}

// Items: mango hero with friends.
const item = (name) => loadImage(`${ART}/Sprites/Items/${name}.png`);
const shadow = (blur, dy) => { g.shadowColor = "rgba(40, 0, 25, 0.35)"; g.shadowBlur = blur; g.shadowOffsetY = dy; };
const place = async (name, x, y, size, rot = 0) => {
  const img = await item(name);
  g.save();
  g.translate(x, y);
  g.rotate((rot * Math.PI) / 180);
  shadow(18, 8);
  g.drawImage(img, -size / 2, -size / 2, size, size);
  g.restore();
};
const flower = await loadImage(`${ART}/UI/Flower.png`);
g.save(); g.globalAlpha = 0.9; g.drawImage(flower, 160, 70, 360, 360); g.restore();
await place("09_rickshaw", 120, 360, 170, -8);
await place("07_chai", 505, 385, 140, 8);
await place("02_nimbu", 150, 140, 110, -14);
await place("04_tamatar", 520, 120, 105, 12);
await place("06_aam", 335, 250, 300, -6);

// Title block on the right.
g.shadowColor = "transparent";
const tx = 790;
g.textAlign = "center";
g.textBaseline = "alphabetic";
g.font = "118px Lilita";
g.lineJoin = "round";
const word = (text, x, fill) => {
  g.lineWidth = 16; g.strokeStyle = "#1B1F4B"; g.strokeText(text, x, 225);
  g.fillStyle = fill; g.fillText(text, x, 225);
};
word("Desi", tx, "#FFB627");
g.font = "118px Lilita";
const merge = "Merge";
g.lineWidth = 16; g.strokeStyle = "#1B1F4B"; g.strokeText(merge, tx, 340);
g.fillStyle = "#FFFFFF"; g.fillText(merge, tx, 340);
// Tagline pill.
g.font = "30px Poppins";
const tag = "Milao • Barhao • Jeeto";
const tw = g.measureText(tag).width + 56;
g.fillStyle = "#1B1F4B";
g.beginPath(); g.roundRect(tx - tw / 2, 372, tw, 56, 28); g.fill();
g.fillStyle = "#FFB627";
g.fillText(tag, tx, 411);

writeFileSync(OUT, await c.encode("png"));
console.log("wrote", OUT);
