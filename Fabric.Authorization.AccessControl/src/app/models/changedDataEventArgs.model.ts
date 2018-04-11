export interface IDataChangedEventArgs {
    memberAffected: string;
    memberType: string;
    action: string;
    changedDataType: string;
    changes: string[];
}
