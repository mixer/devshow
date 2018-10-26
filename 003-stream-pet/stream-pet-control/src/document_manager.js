const EventEmitter = require('eventemitter3');

/**
 * Class that is responsible for managing the document DOM
 * (e.g. rendering the custom control)
 */
class DocumentManager extends EventEmitter {
    constructor(config) {
        super();
        this.config = config;
    }

    /**
     * Moves the video on Mixer to make room for the
     * stream pet pane
     * @param paneWidth 
     */
    arrangeVideo(paneWidth) {
        
    }

    /**
     * Registers click handlers for all action buttons
     * @param possibleActions 
     */
    registerActionClickHandlers(possibleActions) {
        for (let action of possibleActions) {
            document.getElementById(`action-${action}`).onclick = () => {
                this.disableButtons();
                this.emit('actionClicked', action);
                gameManager.doParticipantAction(action);
            };
        }
    }

    /**
     * Updates the DOM elements for the leaderboard
     * @param leaderboard 
     */
    updateLeaderboard(newLeaderboard) {
        const leaderboard = document.getElementById('leaderboard');
        let html = [];
        newLeaderboard.forEach(entry => {
            html.push(`<div class="entry">${entry.username} (${Math.round(entry.points)} points)</div>`);
        });

        leaderboard.innerHTML = html.join('');
    }

    /**
     * Updates the DOM element for the participant
     * @param participant The participant that is being displayed
     * @param possibleStates The possible states for the pet
     */
    participantUpdated(participant, possibleStates) {
        const pet = document.getElementById('pet');
        for (let state of possibleStates) {
            // Update classes on the pet
            if (participant.state === state) {
                if (!pet.classList.contains(state)) {
                    pet.classList.add(state);
                }
            } else {
                pet.classList.remove(state);
            }
        };

        const happinessBar = document.getElementById('happiness-bar');
        happinessBar.style.width = `${participant.health*100}%`;
    }

    /**
     * Gets all of the action button elements from the DOM as an array
     */
    getActions() {
        return [].slice.call(document.getElementsByClassName('action'));
    }

    /**
     * Disables all action buttons for `buttonDisabledDuration`
     */
    disableButtons() {
        for (let button of this.getActions()) {
            button.setAttribute('disabled', true);
        }

        setTimeout(
            () => this.enableButtons(),
            this.config.buttonDisabledDurationMS);
    }

    /**
     * Enables all action buttons
     */
    enableButtons() {
        for (let button of this.getActions()) {
            button.removeAttribute('disabled');
        }
    }
}

module.exports = {DocumentManager};