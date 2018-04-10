export interface IDataChanged {
    member: string;
    actionType: string;
    changes: IChangedData[];
}

export interface IChangedData {
        name: string;
}
