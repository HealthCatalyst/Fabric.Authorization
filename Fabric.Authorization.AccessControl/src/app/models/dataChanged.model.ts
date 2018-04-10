export interface IDataChanged {
    changes: IChangedData[];
}

export interface IChangedData {
    name: string;
    type: string;
    action: string;
}
