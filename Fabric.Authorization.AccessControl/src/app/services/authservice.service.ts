import { Injectable } from '@angular/core';

@Injectable()
export class AuthserviceService {

  constructor() { }


  foo(text: string) : string{
    return "you called foo and passed: " + text;
  }
}
