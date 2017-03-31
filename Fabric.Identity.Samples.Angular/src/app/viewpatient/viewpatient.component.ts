import { Component } from '@angular/core';
import { Http, Headers, RequestOptions } from '@angular/http';
import { AuthService } from '../shared/services/auth.service';
import { User } from 'oidc-client';

@Component({
  selector: 'app-viewpatient',
  templateUrl: './viewpatient.component.html',
  styleUrls: ['./viewpatient.component.css']
})
export class ViewpatientComponent {
    public patientDetails: PatientDetails;
    public errorMessage: string;
    public authenticatedUser: User;

    constructor(http: Http, private authService: AuthService) {
        authService.getUser().then(user => {
            this.authenticatedUser = user;
            let authHeaders = new Headers();
            authHeaders.append('Authorization', 'Bearer ' + user.access_token);
            authHeaders.append('Content-Type', 'application/json');

            let options = new RequestOptions({ headers: authHeaders });

            http.get('http://localhost:5003/patients/123', options).subscribe(
                result => { this.patientDetails = result.json() as PatientDetails; },
                error => { this.errorMessage = <any> error }
            );
        });
    }

  

}

interface PatientDetails {
    firstName: string;
    lastName: string;
    dateOfBirth: Date;
    requestingUserClaims: Claims[]
}

interface Claims {
    type: string;
    value: string;
}
