import { Injectable } from '@angular/core';
import { ErrorHandler } from '@angular/core';
import { LoggingService } from './logging.service';

@Injectable()
export class CustomErrorService extends ErrorHandler {

  constructor(private loggingService: LoggingService) {
    super();
  }

  handleError(error): void {
     this.loggingService.error(error.originalStack || error.message);
     super.handleError(error);
  }
}
