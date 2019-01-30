import * as Mixer from '@mixer/cdk-std';
import * as actions from './actions';

// Import our custom CSS.
require('./style.scss');

// We need to tell Mixer we're ready
Mixer.isLoaded();

// Let's move the video over to give us space for our chartss
Mixer.display.moveVideo({
    top: 0,
    right: 260,
    left: 0,
    bottom: 0,
});

// Finally, we need to listen for changes
Mixer.socket.on('event', (data) => {
    actions.updateMap(data);
});

Mixer.socket.on('onParticipantUpdate', (update) => {
    actions.updateParticipant(update.participants[0]);
});