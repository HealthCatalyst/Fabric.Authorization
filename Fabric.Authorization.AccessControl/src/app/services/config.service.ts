import { Inject, Injectable } from '@angular/core';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Rx';
import { Config } from '../models/config';

import { LoggingService } from './logging.service';

@Injectable()
export class ConfigService{
    config: Config;

    constructor(private http: Http, private loggingService: LoggingService){
        this.config = new Config();        
    }

    loadConfig(){
        var self = this;
        return new Promise((resolve, reject) => {
            this.http.get('assets/appconfig.json')
            .map(res => <Config>res.json() )
            .subscribe(config => {
                self.loggingService.log('configuration loaded......');
                this.config = config;
                resolve();
            });        
        });
    }

}