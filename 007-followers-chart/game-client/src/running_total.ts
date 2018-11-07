import { IUserAttribution } from "./models";

export interface IRunningTotalEvent {
    id: string;
    user: IUserAttribution;
}

export interface IRunningTotalEventSummary {
    id: string;
    count: number;
    users: IUserAttribution[];
}

export class RunningTotal<T extends IRunningTotalEvent> {
    private events: T[] = [];
    private totals: { [id: string]: IRunningTotalEventSummary } = {};

    constructor(
        private keepItems: number,
    ) {}

    public getEventSummaries(): IRunningTotalEventSummary[] {
        return Object.keys(this.totals).map(k => this.totals[k]);
    }

    public addEvent(event: T) {
        this.events.push(event);
        this.addToTotal([event]);
        if (this.events.length > this.keepItems) {
            const remove = this.events.splice(0, this.events.length - this.keepItems);
            this.removeFromTotal(remove);
        }
    }

    public addToTotal(events: T[]) {
        events.forEach(e => {
            if (!this.totals[e.id]) {
                this.totals[e.id] = {
                    id: e.id,
                    count: 0,
                    users: [],
                };
            }

            this.totals[e.id].count++;
            this.totals[e.id].users.push(e.user);
        });
    }

    public removeFromTotal(events: T[]) {
        events.forEach(e => {
            if (!this.totals[e.id]) {
                return;
            }

            if (--this.totals[e.id].count === 0) {
                delete this.totals[e.id];
            } else {
                const removeIndex = this.totals[e.id].users.indexOf(
                    this.totals[e.id].users.find(u => u.userId === e.user.userId));
                this.totals[e.id].users.splice(0, 1);
            }
        });
    }
}