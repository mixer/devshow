import { Carina } from 'carina';
import { BehaviorSubject } from 'rxjs';
import * as ws from 'ws';

export interface IFollowerState {
    timestamp: Date,
    count: number,
}

export type FollowerStateHistory = { [timestamp: number]: IFollowerState };

export class LiveLoader {
    private carina: Carina;

    public init(): void {
        if (!this.carina) {
            Carina.WebSocket = ws;
            this.carina = new Carina({ isBot: true }).open();
        }
    }

    public startFollowerListener(channelId: number, historySize: number): BehaviorSubject<FollowerStateHistory> {
        const subject: BehaviorSubject<FollowerStateHistory> = new BehaviorSubject({});
        const history = {};
        this.carina.subscribe<{ numFollowers: number }>(`channel:${channelId}:update`, data => {
            console.log('constellation', data);
            if (typeof data.numFollowers === 'number') {
                subject.next(
                    this.addToHistory(history, historySize, {
                        count: data.numFollowers,
                        timestamp: new Date(),
                    }));
            }
        });

        return subject;
    }

    private addToHistory(history: FollowerStateHistory, maxSize: number, newItem: IFollowerState) {
        const keys = Object.keys(history);
        if (0 === maxSize) {
            return {};
        }

        if (keys.length >= maxSize) {
            delete history[keys.sort()[0]];
        }

        history[newItem.timestamp.getTime()] = newItem;
        return history;
    }
}