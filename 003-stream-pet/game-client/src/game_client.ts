import { ShortcodeAuthClient, IAccessToken, LocalTokenStore } from 'mixer-shortcode-oauth';
import { GameClient, setWebSocket, IParticipant, IInputEvent, IInput, } from '@mixer/interactive-node';
import { ParticipantState, IPetState } from './participant_state';
import { PointsManager } from './points_manager';
import * as ws from 'ws';
import * as fs from 'fs';

interface AuthToken {
    authToken: string;
    versionId: number;
}

interface ClientInformation {
    clientId: string;
    clientSecret: string;
    versionId: number;
}

export class MinimalGameClient {
    public static CONTROL_FEED = 'feed';
    public static CONTROL_PLAY = 'play';
    public static CONTROL_MIXER = 'mixer';

    public static LEADERBOARD_SIZE = 10;

    public static UPDATE_PER_SECOND = 2;

    public client: GameClient = null;

    private participants: { [userId: string]: ParticipantState } = {};

    private lastUpdate: number;

    private pointsManager: PointsManager;

    constructor(authToken: AuthToken) {
        this.client = new GameClient();
        this.pointsManager = new PointsManager(this, MinimalGameClient.LEADERBOARD_SIZE);

        this.client.on('open', () => this.mixerClientOpened());
        this.client.on('error', (e) => this.gameClientError(e));

        this.client
            .open(authToken)
            .catch(this.gameClientError);
    }

    /**
     * Initializes the Mixer client.
     * Called when the Mixer client opens.
     */
    private mixerClientOpened() {
        // TODO: Register participant join and leave handlers
        this.client.state.on('participantJoin', p => this.participantJoined(p));
        this.client.state.on('participantLeave', (_session, p) => this.participantLeft(p));

        this.client
            .getScenes()
            .then(() => this.createScenes())
            .then(() => this.goLive())
            .catch(this.gameClientError);
    }

    /**
     * The core game loop
     */
    private update() {
        const updateTime = new Date().getTime();
        const deltaTimeSeconds = (updateTime - this.lastUpdate)/1000;
        this.pointsManager.resetLeaderboard();

        // Update the state for each participant
        const updatedParticipants: IPetState[] = [];
        for (const userId in this.participants) {
            const participant = this.participants[userId];
            if (!participant.isActive) {
                continue;
            }

            participant.update(deltaTimeSeconds);
            this.pointsManager.updateParticipantPoints(deltaTimeSeconds, participant);
            updatedParticipants.push(participant.state);
        }

        // Update the world object which contains the leaderboard
        this.client.updateWorld({
            petLeaderboard: this.pointsManager.getParticipantLeaderboard(),
        });

        // Update each participant which contains their individual information
        this.client.updateParticipants({
            participants: updatedParticipants
        });

        // Schedule the next game loop
        this.lastUpdate = updateTime;
        this.scheduleUpdate();
    }

    /**
     * Creates scenes for this control
     */
    private async createScenes() {
        await this.client.createControls({
            sceneID: scenesArray[0].sceneID,
            controls: scenesArray[0].controls
        });

        const controls = [
            MinimalGameClient.CONTROL_FEED,
            MinimalGameClient.CONTROL_PLAY,
            MinimalGameClient.CONTROL_MIXER,
        ];

        // TODO: For Each Control, bind the mousedown event of the button
        for (var control of controls) {
            this.client.state
                .getControl(control)
                .on('mousedown', (event, participant) =>
                     this.inputEvent(event, participant));
        }

        return this.client.updateScenes({ scenes: scenesArray as any });
    }

    /**
     * Called when a participant joins the session
     * @param participant 
     */
    private participantJoined(participant: IParticipant) {
        if (participant.anonymous) {
            // Anon not supported
            return;
        }

        if (!this.participants[participant.userID]) {
            this.participants[participant.userID] = new ParticipantState(this, participant);
        }

        this.participants[participant.userID].setActive(true);
    }

    /**
     * Called when a participant leaves the session
     * @param participant 
     */
    private participantLeft(participant: IParticipant) {
        if (participant.anonymous) {
            // Anon not supported
            return;
        }

        if (this.participants[participant.userID]) {
            this.participants[participant.userID].setActive(false);
        }
    }

    /**
     * Handles an input event from Mixer
     * @param event 
     * @param participant 
     */
    private inputEvent(event: IInputEvent<IInput>, participant: IParticipant) {
        // TODO: Handle input events
        if (!participant.anonymous && this.participants[participant.userID]) {
            this.participants[participant.userID].inputEvent(event);
        }
    }

    /**
     * Schedules the next run of the update method
     */
    private scheduleUpdate() {
        setTimeout(
            () => this.update(),
            (new Date().getTime() - this.lastUpdate) + 1000/MinimalGameClient.UPDATE_PER_SECOND);
    }

    /**
     * Makes the Mixer game client go live and starts the game loop.
     */
    private goLive() {
        this.client.ready()
            .then(() => this.startGameLoop())
            .catch(e => {
                console.error('interactive client error readying: ', e);
                throw e;
            });
    }

    /**
     * Starts the game loop
     */
    private startGameLoop() {
        this.lastUpdate = new Date().getTime();
        this.update();
    }

    /**
     * Called when the game client encounters an error
     * @param error 
     */
    private gameClientError(error: any) {
        console.error('interactive error: ', error);
    }
}

// Update this scene array if needed
const scenesArray = [
    {
      sceneID: "default",
      controls: [
        {
            controlID: MinimalGameClient.CONTROL_FEED,
            kind: "button",
            text: "Feed the Pet",
        },
        {
            controlID: MinimalGameClient.CONTROL_PLAY,
            kind: "button",
            text: "Play with the Pet",
        },
        {
            controlID: MinimalGameClient.CONTROL_MIXER,
            kind: "button",
            text: "Watch Mixer with the pet",
        }
      ]
    }
];

// Place your app info in mixerauth.json, schema is:
// {
//     "clientId": "",
//     "clientSecret": null, /* optional if you don't have a cient secret */
//     "versionId": 0
// }
const authfile = 'mixerauth.json';

setWebSocket(ws);
fs.readFile(authfile, { encoding: 'utf8' }, (error, contents) => {
    if (error) {
        console.error('Error loading auth token: ', error);
        process.exit(1);
    }

    try {
        let authToken = JSON.parse(contents) as ClientInformation;
        if (typeof authToken.clientId !== 'string') {
            throw "clientId was not a string";
        }

        if (typeof authToken.clientSecret !== 'string' && authToken.clientSecret !== null) {
            throw "clientSecret was not a string or null";
        }

        if (typeof authToken.versionId !== 'number') {
            throw "versionId was not a number";
        }

        const authInfo = {
            client_id: authToken.clientId, 
            client_secret: authToken.clientSecret, 
            scopes: [
                'interactive:manage:self',
                'interactive:play',
                'channel:teststream:view:self',
                'interactive:robot:self'
            ]
        };

        const store = new LocalTokenStore(process.cwd() + '/mixertoken.json');
        const auth = new ShortcodeAuthClient(authInfo, store);
        auth.on('code', (code) => {
            console.log(`Go to https://mixer.com/go?code=${code} and enter code ${code}...`);
        });

        auth.on('authorized', (token: IAccessToken) => {
            // @ts-ignore
            const _instance = new MinimalGameClient(
                {
                    authToken: token.access_token,
                    versionId: authToken.versionId
                });
        });

        auth.on('expired', () => {
            console.error('Auth request expired');
            process.exit(1);
        });

        auth.on('declined', () => {
            console.error('Auth request declined');
            process.exit(1);
        });

        auth.on('error', (e: Error) => {
            console.error('Auth error:', e);
            process.exit(1);
        })

        auth.doAuth();
    }
    catch (e) {
        console.error('Error processing token: ', e);
        process.exit(1);
    }
});