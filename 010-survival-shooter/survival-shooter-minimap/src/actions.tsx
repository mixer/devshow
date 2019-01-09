import { BehaviorSubject } from 'rxjs';
import * as Mixer from '@mixer/cdk-std';

interface IPoint {
    x: number,
    y: number,
    enemy: boolean,
    colorHex: string;
    size: number;
}

const enemyLocations: BehaviorSubject<any> = new BehaviorSubject({});
let bombType;

// do some canvas magic
const canvas = (document.getElementById('minimap') as HTMLCanvasElement)
const canvasRect = canvas.getBoundingClientRect();
canvas.width = canvasRect.width;
canvas.height = canvasRect.height;

const ctx = (document.getElementById('minimap') as HTMLCanvasElement).getContext('2d');

const bombBtns = Array.from(document.getElementsByClassName('bomb-btn'));

bombBtns.forEach((btn: HTMLElement) => {
   btn.onclick = function(ev) {
    bombType = btn.dataset.action;
   } 
});

canvas.onclick = function(ev) {
    if (!bombType) {
        return;
    }

    const rect = (ev.target as HTMLElement).getBoundingClientRect();

    const coordinates = translatePointsToCoord(ev.clientX - rect.left, ev.clientY - rect.top, canvas.width, canvas.height);

    console.log('co ', coordinates);
    Mixer.socket.call('giveInput', {
        controlID: 'bomb',
        event: 'bomb',
        bombType,
        location: { x: coordinates.x, y: coordinates.y }
    });
}

enemyLocations
    .subscribe((points: IPoint[]) => {
        // Need to reset on each update so we clear last position
        ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);

        // loop through and draw
        if (points.length) {
            points.forEach(point => {
                drawPoint(point)
            });
        }
    });

export function updateMap(points: IPoint[]) {
    enemyLocations.next(points);
}

const drawPoint = (point: IPoint) => {
    const radius = point.size;

    // translate our points to canvas
    const { x, y } = translatePoints(point.x, point.y, canvas.width, canvas.height);

    // begin draw
    ctx.beginPath();
    ctx.fillStyle = point.colorHex;
    ctx.arc(x, y, radius, 0, Math.PI * 2, true);
    ctx.fill();
    ctx.closePath();
} 

function translatePoints(_x, _y, canvasWidth, canvasHeight) {
    let x, y;
    x = _x * canvasWidth;
    y = (1 - _y) * canvasHeight;
    return { x, y };
}

function translatePointsToCoord(_x, _y, canvasWidth, canvasHeight) {
    let x, y;
    x = _x / canvasWidth;
    y = (1 + _y) / canvasHeight;
    return { x, y };
}