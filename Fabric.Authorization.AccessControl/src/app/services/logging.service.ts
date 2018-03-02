import { Injectable } from '@angular/core';

@Injectable()
export class LoggingService {

  constructor() { }

  messages: string[] = [];

  debug(...logmessages) {
    console.debug(logmessages);
    this.processMessages(logmessages);
  }

  log(...logmessages) {
    console.log(logmessages);
    this.processMessages(logmessages);
  }

  info(... logmessages) {
    console.info(logmessages);
    this.processMessages(logmessages);
  }

  warn(...logmessages) {
    console.warn(logmessages);
    this.processMessages(logmessages);
  }

  error(...logmessages) {
    console.error(logmessages);
    this.processMessages(logmessages);
  }

  clear() {
    this.messages = [];
  }

  processMessages(...logmessages) {
    for (var message of logmessages) {
      for (var innerMessage of message) {
        if (typeof innerMessage === "object") {
          message = JSON.stringify(message, null, 2)
        }
        this.writeToNavPane(message);
      }
    }
  }

  writeToNavPane(message: string) {    
    this.messages.push(message);
  }
}
