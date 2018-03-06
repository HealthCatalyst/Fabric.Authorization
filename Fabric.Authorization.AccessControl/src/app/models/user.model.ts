import { Role } from '../models';

export class User {
    
    public id: string
    public groups: Array<string>;
    public roles: Array<Role>;
    public name: string;

    constructor(       
        public identityProvider: string,
        public subjectId: string
    ) {
    }
}