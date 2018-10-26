const { GameManager } = require('./game_manager');
const { DocumentManager } = require('./document_manager');
const config = require('../config/config.json');

window.addEventListener('load', function initMixer() {
  // Init the game manager
  const gameManager = new GameManager(config);
  const documentManager = new DocumentManager(config);

  // Handle data from Mixer
  mixer.socket.on('onWorldUpdate', w => gameManager.onWorldUpdated(w));
  mixer.socket.on('onParticipantUpdate', p => gameManager.onParticipantUpdated(p));

  // Update the leaderboard
  gameManager.on('leaderboardUpdated', () => {
    documentManager.updateLeaderboard(gameManager.leaderboard);
  });

  // Update the participant
  gameManager.on('participantUpdated', () => {
    documentManager.participantUpdated(
      gameManager.participantData,
      gameManager.possibleStates);
  });

  // Move the video
  mixer.display.moveVideo({
    top: 0,
    bottom: 0,
    left: 0,
    right: config.paneWidth,
  });

  documentManager.registerActionClickHandlers(gameManager.actions);
  documentManager.on('actionClicked', action => {
    mixer.socket.call('giveInput', {
      controlID: action,
      event: 'mousedown'
    });
  });

  // Done all of our setup, call isLoaded to let Mixer know we're G2G!
  mixer.isLoaded();
});
