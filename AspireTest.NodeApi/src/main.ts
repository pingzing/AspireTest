import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import otelSdk from './instrumentation';

async function bootstrap() {
  if (otelSdk) {
    await otelSdk.start();
  }

  const app = await NestFactory.create(AppModule);
  const port = process.env.PORT ?? 3000;
  await app.listen(port);
}
bootstrap();
