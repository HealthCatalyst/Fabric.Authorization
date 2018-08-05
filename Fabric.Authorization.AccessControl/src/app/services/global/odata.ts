export namespace OData {
    export interface IObject<T = any> {
        '@odata.context': string;
    }

    export interface IArray<T = any> extends IObject<T> {
        '@odata.count': number;
        value: T[];
    }
}