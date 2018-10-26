import { MinimalGameClient } from "./game_client";
import { ParticipantState, PetStates, IPetState } from "./participant_state";

export interface IParticipantPoints {
    points: number;
}

export class PointsManager {
    public static GoodPointsPerMinute = 2;
    public static MehPointsPerMinute = 1;

    private leaderboard: IPetState[] = [];

    constructor(
        private gameClient: MinimalGameClient,
        private leaderboardSize: number,
    ) {}

    /**
     * Resets the leaderboard
     * @param leaderboardSize 
     */
    public resetLeaderboard(leaderboardSize?: number): void {
        this.leaderboard = [];
        if (typeof leaderboardSize === 'number') {
            this.leaderboardSize = leaderboardSize;
        }
    }

    /**
     * Called every tick. Updates the number of points the participant has.
     * @param deltaTimeSeconds 
     * @param participant 
     */
    public updateParticipantPoints(deltaTimeSeconds: number, participant: ParticipantState) {
        const deltaMinutes = deltaTimeSeconds/60;
        if (participant.isActive()) {
            participant.state.points += deltaMinutes * this.getParticipantPointModifier(participant);
            this.placeInLeaderboard(participant);
        }
    }

    /**
     * Gets the current leaderboard
     */
    public getParticipantLeaderboard(): IPetState[] {
        return this.leaderboard;
    }

    /**
     * Places the participant in the leaderboard, if they place at all
     * @param participant 
     */
    private placeInLeaderboard(participant: ParticipantState) {
        for (var i = 0; i < this.leaderboard.length; ++i) {
            if (participant.state.points > this.leaderboard[i].points) {
                break;
            }
        }

        this.leaderboard.splice(i, 0, participant.state);
        if (this.leaderboard.length > this.leaderboardSize) {
            this.leaderboard.splice(this.leaderboard.length - 1, this.leaderboard.length - this.leaderboardSize);
        }
    }

    /**
     * Returns true if the participant is in a good state
     * @param participant 
     */
    private isParticipantGoodState(participant: ParticipantState): boolean {
        switch (participant.state.state) {
            case PetStates.Happy:
                return true;
        }

        return false;
    }

    /**
     * Gets the current point earn modifier for the participant
     * @param participant 
     */
    private getParticipantPointModifier(participant: ParticipantState) {
        if (this.isParticipantGoodState(participant)) {
            return PointsManager.GoodPointsPerMinute;
        }

        return PointsManager.MehPointsPerMinute;
    }
}