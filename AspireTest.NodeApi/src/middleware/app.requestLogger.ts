import { Injectable, Logger, NestMiddleware } from '@nestjs/common';
import { Request, Response, NextFunction } from 'express';

@Injectable()
export class RequestLoggerMiddleware implements NestMiddleware {
  private logger = new Logger(RequestLoggerMiddleware.name);

  use(request: Request, response: Response, next: NextFunction): void {
    const requestStart = process.hrtime.bigint();

    response.on('finish', () => {
      const requestEnd = process.hrtime.bigint() - requestStart;
      const requestMs = requestEnd / BigInt(1_000_000_000);
      const path = request.path;
      const { statusCode, statusMessage } = response;
      this.logger.log(
        `HTTP ${statusCode}: ${statusMessage} to '${path}' completed in ${requestMs}ms`,
      );
    });

    next();
  }
}
