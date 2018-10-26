const EventEmitter = require('eventemitter3');

/**
 * Class that is responsible for managing game state.
 */
class GameManager extends EventEmitter {
    constructor(config) {
        super();
        this.config = config;
        this.actions = config.actions;
        this.possibleStates = config.possibleStates;
        this.leaderboard = [];
        this.participantData = {
            points: 0,
            health: 0,
            state: config.defaultState,
        };
    }

    /**
     * Called when the world object is updated
     */
    onWorldUpdated({ petLeaderboard }) {
        if (!petLeaderboard) {
            return;
        }

        if (this.leaderboardHasChanged(petLeaderboard)) {
            this.leaderboardUpdated(petLeaderboard);
        }
    }

    /**
     * Called when the active participant data is updated
     */
    onParticipantUpdated({ participants: [participant] }) {
        if (!participant) {
            return;
        }

        if (this.participantHasChanged(participant)) {
            this.participantData = participant;
            this.emit('participantUpdated');
        }
    }

    /**
     * Helper method for when the leaderboard has changed.
     * Emits `leaderboardUpdated` event.
     * @param newLeaderboard The new leaderboard
     */
    leaderboardUpdated(newLeaderboard) {
        this.leaderboard = newLeaderboard;
        this.emit('leaderboardUpdated');
    }

    /**
     * Helper method that compares `petLeaderboard` to the current leaderboard
     * and returns true if it has changed
     * @param petLeaderboard 
     */
    leaderboardHasChanged(petLeaderboard) {
        // If the size of the leaderboard has changed, do a full refresh
        if (petLeaderboard.length !== this.leaderboard.length) {
            return true;
        } else {
            // Otherwise detect if any of the elements have changed
            for (var i = 0; i < this.leaderboard.length; ++i) {
                if (this.leaderboard[i].userID !== petLeaderboard[i].userID
                        || Math.round(this.leaderboard[i].points) !== Math.round(petLeaderboard[i].points)) {
                    return true;
                }
            }
        }

        return false;
    }

    /**
     * Helper method that compares `newParticipant` to the current participant
     * and returns true if it has changed
     * @param newParticipant 
     */
    participantHasChanged(newParticipant) {
        for (var prop in this.participantData) {
            if (this.participantData[prop] !== newParticipant[prop]) {
                return true;
            }
        }

        return false;
    }
}

module.exports = {GameManager};