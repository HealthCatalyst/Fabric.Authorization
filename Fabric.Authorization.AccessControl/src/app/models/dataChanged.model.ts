export interface IDataChanged {
    member: string;
    action: string;
    type: string;
    changes: IChangedData[];
}

export interface IChangedData {
        name: string;
}
