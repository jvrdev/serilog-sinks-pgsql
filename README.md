# Serilog.Sinks.Pgsql

A Serilog sink that writes log events to PostgreSQL. This allows access to centralized structured logs in a very approachable manner. The sink leverages PostgreSQL's NoSQL capabilities in a configurable way, you decide how much log event information you save into dedicated columns and how much into the LogEvent JSON document.

**Platforms** - .NET Standard 1.6

## Configuration

At minimum a connection string and table name are required.

#### Sample Code

```csharp
    var columnOptions = new ColumnOptions(additionalDataColumns: 
    new[]
    {
        new DataColumnMapping("Service", NpgsqlDbType.Text),
        new DataColumnMapping("MachineName", NpgsqlDbType.Text)
    });
var config = new Serilog.LoggerConfiguration()
    .Enrich.WithProperty("Service", "Tests")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Pgsql("Host=localhost;Database=logs_test;Username=postgres;Password=wblyStOLVnpyGKYv7sWr", "Logs", columnOptions: columnOptions);
using (var logger = config.CreateLogger())
{
    logger.Information("Hello world {@Bottle}", new { Liquid = "Water", Price = new { Amount = 5.0, Currency = "Euro" } });
}
```

## Table definition

You'll need to create a table like this in your database:

```
CREATE TABLE public."Logs"
(
    "Id" integer NOT NULL DEFAULT nextval('"Logs_Id_seq"'::regclass),
    "Message" text COLLATE pg_catalog."default",
    "MessageTemplate" text COLLATE pg_catalog."default",
    "Level" text COLLATE pg_catalog."default",
    "TimeStamp" timestamp with time zone NOT NULL,
    "Exception" text COLLATE pg_catalog."default",
    "Properties" jsonb,
    "SourceContext" text COLLATE pg_catalog."default",
    "Service" text COLLATE pg_catalog."default" NOT NULL,
    "MachineName" text COLLATE pg_catalog."default" NOT NULL,
    "LogEvent" jsonb,
    CONSTRAINT "Logs_pkey" PRIMARY KEY ("Id")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public."Logs"
    OWNER to postgres;
```

### Options for serialization of the log event data

#### JSON (LogEvent column)

The log event JSON can be stored to the LogEvent column. This can be enabled by adding the LogEvent column to the `columnOptions.Store` collection. Use the `columnOptions.LogEvent.ExcludeAdditionalProperties` parameter to exclude redundant properties from the JSON.
