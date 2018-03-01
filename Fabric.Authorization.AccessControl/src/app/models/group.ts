import { Role } from './Role';
import { User } from './User';

export class Group {
    
    public id: string
    public roles: Array<Role>;
    public users: Array<User>;

    constructor(       
        public groupName: string,
        public groupSource: string
    ) {
    }
}