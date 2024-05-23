import { Injectable } from '@nestjs/common';
import { OtelMethodCounter } from 'nestjs-otel';

@Injectable()
export class AppService {
  @OtelMethodCounter()
  getHello(): string {
    return 'Hello World!';
  }
}
