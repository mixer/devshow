import { ShortcodeAuthClient, IAccessToken, LocalTokenStore } from 'mixer-shortcode-oauth';
import { GameClient, setWebSocket, IParticipant, IInputEvent, IInput, } from '@mixer/interactive-node';
import { LiveLoader, FollowerStateHistory } from './live_loader';
import * as Mixer from '@mixer/client-node';
import * as ws from 'ws';
import * as fs from 'fs';
import { RunningTotal, IRunningTotalEvent } from './running_total';

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
    public static HISTORY_SIZE = 10;
    public static RUNNING_TOTAL_SIZE = 10;

    public gameClient: GameClient = null;

    public mixerClient: Mixer.Client;

    private lastUpdate: number;

    private liveLoader: LiveLoader;

    private runningTotals: RunningTotal<IRunningTotalEvent>;

    constructor(authToken: AuthToken, mixerClient: Mixer.Client) {
        this.mixerClient = mixerClient;
        this.gameClient = new GameClient();
        this.liveLoader = new LiveLoader();
        this.liveLoader.init();
        this.runningTotals = new RunningTotal<IRunningTotalEvent>(MinimalGameClient.RUNNING_TOTAL_SIZE);

        setInterval(() => {
            this.runningTotals.addEvent({
                id: Math.round(Math.random() * 10).toString(),
                user: {
                    userId: 1,
                    name: 'mobius5150',
                }
            });
            console.log(this.runningTotals.getEventSummaries());
        }, 2000);

        this.gameClient.on('open', () => this.mixerClientOpened());
        this.gameClient.on('error', (e) => this.gameClientError(e));

        this.gameClient
            .open(authToken)
            .catch(this.gameClientError);
    }

    /**
     * Initializes the Mixer client.
     * Called when the Mixer client opens.
     */
    private mixerClientOpened() {
        this.mixerClient
            .request<{ channel: { id: number } }>('GET', 'users/current')
            .then(user => this.liveLoader.startFollowerListener(user.body.channel.id, MinimalGameClient.HISTORY_SIZE))
            .then(subject => subject.subscribe(h => this.historyUpdated(h)))
            .then(() => console.log('Started follower listener'))
            .catch(this.gameClientError);

        this.gameClient
            .getScenes()
            .then(() => this.goLive())
            .catch(this.gameClientError);
    }

    /** 
     * Called when the follower history is updated. Updates the world state.
     */
    private historyUpdated(history: FollowerStateHistory): void {
        console.log('History updated:', history);

        this.gameClient.updateWorld({
            history
        });
    }

    /**
     * Makes the Mixer game client go live and starts the game loop.
     */
    private goLive() {
        this.gameClient.ready()
            .then(() => console.log('game client connected'))
            .catch(e => {
                console.error('interactive client error readying: ', e);
                throw e;
            });
    }

    /**
     * Called when the game client encounters an error
     * @param error 
     */
    private gameClientError(error: any) {
        console.error('interactive error: ', error);
    }
}

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
            const mixerClient = new Mixer.Client();

            // @ts-ignore
            mixerClient.use(new Mixer.OAuthProvider(this.mixerClient, {
                clientId: authInfo.client_id,
                tokens: {
                    access: token.access_token,
                    expires: token.expires_at.toString(),
                }
            }))

            // @ts-ignore
            const _instance = new MinimalGameClient(
                {
                    authToken: token.access_token,
                    versionId: authToken.versionId
                }, mixerClient);
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