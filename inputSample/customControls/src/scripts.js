var gamepadButtons = {
  A: 0,
  B: 1,
  X: 2,
  Y: 3,
  LB: 4,
  RB: 5,
  LT: 6,
  RT: 7,
  View: 8,
  Menu: 9,
  LeftStick: 10,
  RightStick: 11,
  Up: 12,
  Down: 13,
  Left: 14,
  Right: 15
};

var keyHeld = {};
var gamepadHeld = {};
var leftStick = {
    x: 0,
    y: 0
};

var rightStick = {
    x: 0,
    y: 0
};

var gamepadAxes = {
  LeftStickHorizontal: 0,
  LeftStickVerticalInverted: 1,
  RightStickHorizontal: 2,
  RightStickVerticalInverted: 3
};

function updateGamepadInput() {
  var gamepads = navigator.getGamepads
      ? navigator.getGamepads()
      : navigator.webkitGetGamepads
          ? navigator.webkitGetGamepads
          : [];
  if (!gamepads) {
      return;
  }

  var gp = gamepads[0];
  if (!gp) {
      return;
  }

  var keys = Object.keys(gamepadButtons);
  for (var i = 0; i < keys.length; i++) {
      var key = keys[i];
      var pressed = buttonPressed(gp.buttons[gamepadButtons[key]]);

      var wasPressed = gamepadHeld[key];

      if (pressed && !wasPressed) {
          onGamepadButtonDown(key);
      } else if (!pressed && wasPressed) {
          onGamepadButtonUp(key);
      }

      if (!pressed) {
          delete gamepadHeld[key];
      } else {
          gamepadHeld[key] = pressed;
      }
  }

  // hacky positioning on the analog stick images
  // could use images with better origins to mitigate some of this...
  leftStick.x = axisValue(gp.axes[gamepadAxes.LeftStickHorizontal]);
  leftStick.y = -axisValue(gp.axes[gamepadAxes.LeftStickVerticalInverted]);

  var leftStickRads = Math.atan2(-leftStick.y, leftStick.x);
  var leftStickMag = magnitude(leftStick.x, leftStick.y) * 100;

  $('#LeftAnalog').css({
    transform: 'rotate(' + (leftStickRads * 180) / Math.PI + 180 + 'deg)',
    width: leftStickMag + 'px',
    height: 70 + 'px',
    top: 60,
    left: 117 -leftStickMag/2
  });

  rightStick.x = axisValue(gp.axes[gamepadAxes.RightStickHorizontal]);
  rightStick.y = -axisValue(gp.axes[gamepadAxes.RightStickVerticalInverted]);

  var rightStickRads = Math.atan2(-rightStick.y, rightStick.x);
  var rightStickMag = magnitude(rightStick.x, rightStick.y) * 100;

  $('#RightAnalog').css({
    transform: 'rotate(' + (rightStickRads * 180) / Math.PI + 180 + 'deg)',
    width: rightStickMag + 'px',
    height: 70 + 'px',
    top: 125,
    left: 293 -rightStickMag/2
  });

};

function magnitude(a, b) {
  return Math.sqrt(a * a + b * b);
};

function buttonPressed(b) {
  if (typeof b == 'object') {
      return b.pressed;
  }
  return b == 1.0;
};

function axisValue(a) {
  if (typeof a == 'object') {
      return a.value;
  }
  return a;
};

function onGamepadButtonDown(button) {
  $('#' + button).removeClass('hidden');
};

function onGamepadButtonUp(button) {
  $('#' + button).addClass('hidden');
};

window.addEventListener('load', function initMixer() {
  $('#LeftAnalog').css({
    width: 0,
    height: 0
  });

  $('#RightAnalog').css({
    width: 0,
    height: 0
  });

  // poll for input at 60hz
  setInterval(updateGamepadInput, 16);
  mixer.isLoaded();
});
