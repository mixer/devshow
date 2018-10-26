import { IParticipant, IButton, IButtonInput, IInputEvent, IInput } from '@mixer/interactive-node';
import { MinimalGameClient } from './game_client';
import { IParticipantPoints } from './points_manager';

export enum PetStates {
    Happy = 'happy',
    Hungry = 'hungry',
    Sad = 'sad',
    Bored = 'bored',
}

export interface IPetState extends IParticipantPoints, IParticipant {
    health: number;
    state: PetStates;
    points: number;
}

export class ParticipantState {
    private static UPDATE_BIG: number = 0.4;
    private static UPDATE_SMALL: number = 0.1;
    private static DECAY_PER_SECOND: number = 1/240;
    private static INITIAL_HEALTH: number = 0.5;
    private static CHANCE_STATE_CHANGE: number = 1;
    private static STATE_CHANGE_SECONDS: number = 4;
    private static STATE_KEYS = Object.keys(PetStates);

    public state: IPetState;
    private active: boolean = true;
    
    constructor(private gameManager: MinimalGameClient, private participant: IParticipant) {
        this.state = {
            ...participant,
            health: ParticipantState.INITIAL_HEALTH,
            state: PetStates.Happy,
            points: 0,
        };
    }

    /**
     * Updates the participant state every game loop
     * @param deltaTimeSeconds The fractional seconds since the last time the method was called
     */
    public update(deltaTimeSeconds: number) {
        if (!this.active) {
            return;
        }

        if (deltaTimeSeconds > 0) {
            this.updateHealth(-ParticipantState.DECAY_PER_SECOND * deltaTimeSeconds);
        }

        const changeProbability = deltaTimeSeconds/ParticipantState.STATE_CHANGE_SECONDS
            * ParticipantState.CHANCE_STATE_CHANGE
            * (1-this.state.health);
        if (this.state.state === PetStates.Happy && Math.random() <= changeProbability) {
            while (this.state.state === PetStates.Happy) {
                this.state.state = PetStates[ParticipantState.STATE_KEYS[Math.round(Math.random() * ParticipantState.STATE_KEYS.length)]];
            }
        }
    }

    /**
     * Called when an input event is received from Mixer for this participant
     * @param event 
     */
    public inputEvent(event: IInputEvent<IInput>) {
        if (!this.active) {
            return;
        }

        switch (event.input.controlID) {
            case MinimalGameClient.CONTROL_FEED:
                if (this.state.state === PetStates.Hungry) {
                    this.updateHealth(ParticipantState.UPDATE_BIG);
                    this.happyState();
                } else {
                    this.updateHealth(ParticipantState.UPDATE_SMALL);
                }
                break;

            case MinimalGameClient.CONTROL_PLAY:
                if (this.state.state === PetStates.Bored) {
                    this.updateHealth(ParticipantState.UPDATE_BIG);
                    this.happyState();
                } else {
                    this.updateHealth(ParticipantState.UPDATE_SMALL);
                }
                break;

            case MinimalGameClient.CONTROL_MIXER:
                if (this.state.state === PetStates.Sad) {
                    this.updateHealth(ParticipantState.UPDATE_BIG);
                    this.happyState();
                } else {
                    this.updateHealth(ParticipantState.UPDATE_SMALL);
                }
                break;
        }
    }

    /**
     * Sets whether the participant is active
     * @param active 
     */
    public setActive(active: boolean) {
        this.active = active;
    }

    /**
     * Gets whether the participant is active
     */
    public isActive(): boolean {
        return this.active;
    }

    /**
     * Update the participants health by a certain amount.
     * Ensures that the health bar remains between [0,1]
     * @param by 
     */
    private updateHealth(by: number) {
        this.state.health += by;
        if (this.state.health < 0) {
            this.state.health = 0;
        } else if (this.state.health > 1) {
            this.state.health = 1;
        }
    }

    /**
     * Goes to our happy place
     */
    private happyState() {
        this.state.state = PetStates.Happy;
    }
}