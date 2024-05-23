import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-grpc';
import { SimpleLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { HttpInstrumentation } from '@opentelemetry/instrumentation-http';
import { credentials } from '@grpc/grpc-js';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';

const environment = process.env.NODE_ENV || 'development';

const otlpServer = process.env.OTEL_EXPORTER_OTLP_ENDPOINT;

let otelSdk: NodeSDK | null = null;

if (otlpServer) {
  console.log(`Sending OTLP stuff to ${otlpServer}`);

  const isHttps = otlpServer.startsWith('https://');
  const collectorOptions = {
    credentials: isHttps
      ? credentials.createSsl()
      : credentials.createInsecure(),
  };

  otelSdk = new NodeSDK({
    traceExporter: new OTLPTraceExporter(collectorOptions),
    metricReader: new PeriodicExportingMetricReader({
      exportIntervalMillis: environment === 'development' ? 5_000 : 10_000,
      exporter: new OTLPMetricExporter(collectorOptions),
    }),
    logRecordProcessor: new SimpleLogRecordProcessor(
      new OTLPLogExporter(collectorOptions),
    ),
    instrumentations: [
      getNodeAutoInstrumentations(),
      new HttpInstrumentation(),
    ],
  });
}

export default otelSdk;

process.on('SIGTERM', () => {
  otelSdk
    .shutdown()
    .then(
      () => console.log('Open Telemetry SDK shut down successfully'),
      (err) => console.log('Failed to shut down Open Telemetry SDK: ', err),
    )
    .finally(() => process.exit(0));
});
