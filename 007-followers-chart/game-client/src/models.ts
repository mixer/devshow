export interface ISkillSummary {
    name: string;
    id: string;
    count: number;
    users: IUserAttribution[];
    icon_url?: string;
}

export interface IUserAttribution {
    userId: number;
    name: string;
}