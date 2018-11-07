import * as Mixer from '@mixer/cdk-std';
import * as actions from './actions';
import { FollowerStateHistory } from '../../game-client/src/live_loader';

// Import our custom CSS.
require('./style.scss');

// We need to tell Mixer we're ready
Mixer.isLoaded();

// Let's move the video over to give us space for our chartss
Mixer.display.moveVideo({
    top: 0,
    right: 350,
    left: 0,
    bottom: 0,
});

// Let's setup our chart
actions.initChart();

// Finally, we need to listen for changes
Mixer.socket.on('onWorldUpdate', (world) => {
    if (world.history) {
        actions.updateHistory(world.history);
    }
});

