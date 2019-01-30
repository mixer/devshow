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
let controllingObjectId = null;
const handleKeys = new Map<string, (ev: string) => void>();

// do some canvas magic
const canvas = (document.getElementById('minimap') as HTMLCanvasElement)
const canvasRect = canvas.getBoundingClientRect();
canvas.width = canvasRect.width;
canvas.height = canvasRect.height;

const ctx = (document.getElementById('minimap') as HTMLCanvasElement).getContext('2d');

const bombBtns = Array.from(document.getElementsByClassName('bomb-btn'));
const controllerContainer = document.getElementById('controller-container');
const takeControlButton = document.getElementById('btn-take-control');
const controlBtns = Array.from(document.getElementsByClassName('control-btn'));

takeControlButton.onclick = (ev) => {
    Mixer.socket.call('giveInput', {
        controlID: 'control',
        event: 'mousedown'
    });

    takeControlButton.setAttribute('disabled', 'disabled');
    setTimeout(() => {
        takeControlButton.removeAttribute('disabled');
    }, 5000);
};

function controlButtonHandler(btn: string, ev: string) {
    if (!controllingObjectId) {
        return;
    }

    Mixer.socket.call('giveInput', {
        controlID: 'control-'+ btn,
        event: ev
    });
}

controlBtns.forEach((btn: HTMLElement) => {
    let state = 'unknown';
    btn.onmousedown = () => controlButtonHandler(btn.dataset.action, 'mousedown');
    btn.onmouseup = () => controlButtonHandler(btn.dataset.action, 'mouseup');
    if (btn.dataset.key) {
        handleKeys.set(btn.dataset.key, (ev) => {
            if (state != ev) {
                state = ev;
                controlButtonHandler(btn.dataset.action, ev);
            }
        });
    }
});

function documentKeyPressHandler(e: KeyboardEvent, ev: string) {
    if (handleKeys.has(e.key)) {
        handleKeys.get(e.key)(ev);
    }
}

document.onkeydown = (e) => documentKeyPressHandler(e, 'mousedown');
document.onkeyup = (e) => documentKeyPressHandler(e, 'mouseup');

export function updateParticipant(participant) {
    if (participant.controllingObjectId && participant.controllingObjectId !== null) {
        controllingObjectId = participant.controllingObjectId;
        controllerContainer.classList.add('active');
    } else {
        controllingObjectId = null;
        controllerContainer.classList.remove('active');
    }
}

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
    Mixer.socket.call('giveInput', {
        controlID: 'bomb',
        event: 'bomb',
        bombType,
        location: { x: coordinates.x, y: coordinates.y }
    });
}

const savedPoints: { [key:string]: IPoint } = {};
enemyLocations
    .subscribe((points: { [key:string ]: IPoint }) => {
        // Need to reset on each update so we clear last position
        ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);

        // Update state
        Object.keys(points).forEach(instanceId => {
            console.log("Point", points[instanceId]);
            if (points[instanceId]) {
                savedPoints[instanceId] = points[instanceId];
            } else {
                delete savedPoints[instanceId];
            }
        });

        // Loop through and draw
        Object.keys(savedPoints).forEach(instanceId => {
            drawPoint(savedPoints[instanceId], instanceId == controllingObjectId);
        });
    });

export function updateMap(points: { [key:string ]: IPoint }) {
    enemyLocations.next(points);
}

const drawPoint = (point: IPoint, controlled: boolean) => {

    const radius = controlled ? 5 : point.size;

    // translate our points to canvas
    const { x, y } = translatePoints(point.x, point.y, canvas.width, canvas.height);

    // begin draw
    ctx.beginPath();
    ctx.fillStyle = controlled ? '#00FF00' : point.colorHex;
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