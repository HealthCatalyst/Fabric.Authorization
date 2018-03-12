export class FabricPrincipal {

    public subjectId: string;
    public firstName: string;
    public middleName: string;
    public lastName: string;
    public principalType: string;

    public name = `${this.firstName} ${this.middleName} ${this.lastName}`

    constructor() { }
}