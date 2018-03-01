export class User {
    
    public id: string
    public groups: Array<string>;
    public name: string;

    constructor(       
        public identityProvider: string,
        public subjectId: string
    ) {
    }
}